using Microsoft.AspNetCore.Mvc;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models;
using SmartPropertySuite.Models.DatabaseModels;
using StackExchange.Redis;
using System.Text.Json;

namespace SmartPropertySuite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IRedisService _redis;
        private readonly IGoogleCalendarService _calendar;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;
        private readonly IChatDetails _chatDetails;
        private readonly IFCMPushNotificationService _pushNotificationService;

        public ChatbotController(IChatService chatService, IRedisService redis, IGoogleCalendarService calendar, ITokenService tokenService, ApplicationDbContext.ApplicationDbContext dbContext, IChatDetails chatDetails, IFCMPushNotificationService pushNotificationService)
        {
            _chatService = chatService;
            _redis = redis;
            _calendar = calendar;
            _tokenService = tokenService;
            _dbContext = dbContext;
            _chatDetails = chatDetails;
            _pushNotificationService = pushNotificationService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var users = _dbContext.CRMPropertySuiteUserInfo.ToList();

                foreach (var user in users)
                {
                    if (user.ExpiryTime <= DateTime.Now)
                    {
                        await _tokenService.RefreshAccessTokenAsync(user);
                    }
                }

                var chat = await _chatDetails.GetChatByChatId(request.ChatId);

                if (chat == null)
                {
                    var newChat = new CRMPropertySuiteUserChats
                    {
                        ChatId = request.ChatId,
                        Conversations = new List<CRMPropertySuiteUserConversations>(),
                        CreatedAt = DateTime.Now,
                        Email = request.UserEmail
                    };

                    await _chatDetails.AddChat(newChat);

                    chat = await _chatDetails.GetChatByChatId(request.ChatId);
                }

                var conversation = _dbContext.CRMPropertySuiteUserConversations.Where(x => x.ChatId == chat.ChatId && x.Status == 1).FirstOrDefault();

                if (request.IsNewConversation)
                {
                    //Clear old state if it's a new conversation
                    await _redis.RemoveStateAsync(request.UserEmail);

                    var newConversation = new CRMPropertySuiteUserConversations
                    {
                        ChatId = request.ChatId,
                        StartedAt = DateTime.Now,
                        ConversationId = request.ConversationId,
                        ExtractionResult = string.Empty,
                        Messages = new List<CRMPropertySuiteUserChatMessages>(),
                        Status = 1 // Open status
                    };

                    if (conversation != null)
                    {
                        conversation.Status = 0;
                        conversation.EndedAt = DateTime.Now;
                    }

                    _dbContext.CRMPropertySuiteUserConversations.Add(newConversation);

                    _dbContext.SaveChanges();

                    conversation = _dbContext.CRMPropertySuiteUserConversations.Where(x => x.ChatId == chat.ChatId && x.Status == 1).FirstOrDefault();
                }

                var messages = _dbContext.CRMPropertySuiteUserChatMessages.Where(c => c.ConversationId == request.ConversationId).ToList();

                //add the message from user to db
                var newMessage = new CRMPropertySuiteUserChatMessages
                {
                    ConversationId = request.ConversationId,
                    MessageId = Guid.NewGuid(),
                    MessageText = request.Message,
                    Sender = request.Sender.ToLower(),
                    SentAt = DateTime.Now
                };

                await _chatDetails.AddMessages(new List<CRMPropertySuiteUserChatMessages> { newMessage });

                messages = await _chatDetails.GetMessagesById(request.ConversationId);

                var eachMessages = _dbContext.CRMPropertySuiteUserChatMessages.Where(x => x.ConversationId == request.ConversationId && x.Sender == "user").Select(x => x.MessageText).ToList();

                chat.ChatTitle = await _chatService.GenerateChatTitleAsync(eachMessages);

                var state = await _redis.GetStateAsync(request.UserEmail);
                var input = request.Message;

                var extraction = await _chatService.ExtractUserDetailsAsync(input);

                // 2. Update only missing parts
                if (!string.IsNullOrWhiteSpace(extraction.IssueType)) state.IssueType = extraction.IssueType;
                if (!string.IsNullOrWhiteSpace(extraction.Priority)) state.Priority = extraction.Priority;
                if (!string.IsNullOrWhiteSpace(extraction.ContactEmail)) state.ContactEmail = extraction.ContactEmail;
                if (extraction.PreferredSlotIndex.HasValue) state.PreferredSlotIndex = extraction.PreferredSlotIndex;
                await _redis.SaveStateAsync(request.UserEmail, state);

                //update conversation exreaction in DB
                var updateConversation = new CRMPropertySuiteUserConversations
                {
                    ChatId = conversation!.ChatId,
                    ConversationId = conversation.ConversationId,
                    ExtractionResult = JsonSerializer.Serialize<ChatState>(state, new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true }),
                    EndedAt = conversation.EndedAt,
                    Status = conversation.Status,
                };

                await _chatDetails.AddOrUpdateCoversations(new List<CRMPropertySuiteUserConversations> { updateConversation });

                if (state.IsComplete())
                {
                    var slots = state.AvailableSlots.Count > 0 ? state.AvailableSlots : await _calendar.GetFreeSlotsAsync(state.Priority);
                    if (!slots.Any()) return Ok("No available slots found.");

                    var chosenSlot = slots.ElementAtOrDefault(state.PreferredSlotIndex!.Value - 1);
                    if (chosenSlot == null) return Ok("Invalid slot selected.");

                    await _calendar.BookAppointmentAsync(state, chosenSlot);
                    string prompt = $"Appointment booked for {chosenSlot.Start}. Let me know if you have another issue!";

                    var slotDate = chosenSlot.Start.ToLocalTime().ToString("f");

                    var confirmationPrompt = $"""
                        You are a helpful assistant for a property management company.
                        A user has just booked a maintenance appointment for {slotDate}.

                        Reply with a friendly confirmation message.

                        Below is **just an example** of how to confirm the booking. Use it as a reference to create a similar, polite, and natural variation. You may rephrase creatively, as long as the meaning stays the same:

                        "Great! Your appointment for {slotDate} is confirmed. Let me know if you need help with anything else."

                        Guidelines:
                        - Keep the response polite and context-aware.
                        - Do **not** ask for more details.
                        - Avoid repeating the example exactly unless it fits naturally.
                        """;

                    var confirmation = await _chatService.GetBotReplyAsync(prompt, confirmationPrompt);
                    var message = new CRMPropertySuiteUserChatMessages
                    {
                        ConversationId = request.ConversationId,
                        MessageId = Guid.NewGuid(),
                        MessageText = confirmation,
                        Sender = "assistant",
                        SentAt = DateTime.Now
                    };

                    var closeConversation = new CRMPropertySuiteUserConversations
                    {
                        ChatId = chat.ChatId,
                        ConversationId = request.ConversationId,
                        Status = 0, // Closed status
                        EndedAt = DateTime.Now,
                        ExtractionResult = JsonSerializer.Serialize<ChatState>(state, new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true })
                    };

                    await _chatDetails.AddOrUpdateCoversations(new List<CRMPropertySuiteUserConversations> { closeConversation });

                    await _chatDetails.AddMessages(new List<CRMPropertySuiteUserChatMessages> { message });

                    var responseMessage = new ChatResponse
                    {
                        ConfirmationMessage = confirmation,
                        UserIssueJson = DeserializeJson(state),
                    };

                    await _redis.RemoveStateAsync(request.UserEmail);

                    state = new ChatState();

                    var notification = _pushNotificationService.GetFCMMobileDeviceInfoByEmail(request.UserEmail);

                    var pushNotification = new FCMPushNotification
                    {
                        Body = $"{slotDate} has been booked.",
                        Title = "Appointment booked.",
                        Email = notification.Email,
                        DeviceId = notification.DeviceId,
                        IsAndroidDevice = notification.IsAndroidDevice,
                        ChatId = chat.ChatId
                    };

                    SendPushnotification(pushNotification);

                    return Ok(responseMessage);
                }

                var stateJson = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });

                var systemPrompt = $@"
                You are a helpful assistant for a property‑management company.  

                You must classify the user’s input and return a JSON object in this format:
                {{
                  ""classification"": ""GreetingOnly"" | ""GreetingWithIssue"" | ""IssueOnly"" | ""Unknown"",
                  ""reply"": ""<your polite, dynamic response>""
                }}

                Current conversation state (JSON):
                {stateJson}
                
                Classification Rules:
                1. If the message is a **greeting only** (for example, “Hi”, “Hello”, “Hey”, “Good morning”, you can identify greetings only on your own as well) and does NOT mention any issue, classify as **GreetingOnly**.
                2. If the message contains both a **greeting and issue details**, classify as **GreetingWithIssue**.
                3. If the message contains only issue-related info (like Plumbing, Electrical, Broken, Repair or any other property related issue), classify as **IssueOnly**.
                4. If the message is unclear or irrelevant, classify as **Unknown**.

                Response Rules:
                 - If classification is **GreetingOnly**:
                   - Reply with a polite and natural greeting.
                   - Below are **just a few examples** of how to greet user back. Use them as a reference to create similar but natural and polite variations. You may also rephrase creatively, as long as the meaning remains the same.
                     - “Hi there! How can I help you today?”
                     - “Good morning! Let me know if there’s anything you need assistance with.”
                     - “Hello! I’m here to help with any maintenance issues you’re experiencing.”
                     - “Hey! How can I assist you with your maintenance request today?”
                   - Try to use different phrasing each time to sound conversational and natural. Avoid repeating the above examples verbatim unless it feels appropriate.
                   - Do not continue with the data collection.

                 - If classification is **GreetingWithIssue** or **IssueOnly**, respond according to the current state:
                   - Ask only about the **first missing field** from this list:
                     1. IssueType
                     2. Priority
                     3. ContactEmail
                     4. PreferredSlotIndex
                 - If message is **GreetingWithIssue** or **IssueOnly**, continue the data collection flow. Ask only about the **first missing field** in the list.
                 - Use the conversation state (shown below) to know what’s missing:
                  {stateJson}
                 - If all fields are present, confirm the booking and end the flow. Do not ask additional questions.
                 - When asking about any missing field (e.g., issue type, priority, contact email, preferred slot), vary your wording each time. Do **not** repeat the same sentence structure or phrasing.
                    - Below are **just a few examples** of how to ask each field. Use them as a reference to create similar but natural and polite variations. You may also rephrase creatively, as long as the meaning remains the same.
                    - For **IssueType**:
                        - “What kind of maintenance issue are you facing?”
                        - “Could you let me know if the problem is plumbing, electrical, or something else?”
                        - “What needs attention today—plumbing, electrical, or another area?”
                    - For **Priority**:
                        - “How urgent is this issue? Low, medium, or high?”
                        - “Can you tell me the priority level of this request?”
                        - “Would you say this issue is low, medium, or high priority?”
                    - For **ContactEmail**:
                        - “Please share your email so we can confirm your appointment.”
                        - “What’s the best email address to reach you for the booking confirmation?”
                        - “Could you provide your email address for confirmation purposes?”
                    - For **PreferredSlotIndex**:
                        - “Which of the available time slots works best for you? Please mention the number.”
                        - “Let me know the index of your preferred time slot.”
                        - “Which slot would you like to book? Reply with the slot number.”
                    -Try to use different phrasing each time to sound conversational and natural. Avoid repeating the above examples verbatim unless it feels appropriate.

                 - Responses must be polite, natural, and professional. Do not sound robotic. Vary tone slightly to avoid repetition. Responses should be complete but not overly long — aim for natural conversational flow.
                Style:
                 - Responses must sound polite, helpful, and professional.
                 - Avoid robotic or repetitive responses.
                 - Responses should be natural in tone and vary in structure each time.
                ";

                var gptResult = await _chatService.GetBotReplyAsync(input, systemPrompt);
                var response = JsonSerializer.Deserialize<GptResponse>(gptResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                if (response.Classification == "GreetingOnly")
                {
                    var message = new CRMPropertySuiteUserChatMessages
                    {
                        ConversationId = request.ConversationId,
                        MessageId = Guid.NewGuid(),
                        MessageText = response.Reply,
                        Sender = "assistant",
                        SentAt = DateTime.Now
                    };

                    await _chatDetails.AddMessages(new List<CRMPropertySuiteUserChatMessages> { message });

                    var responseMessage = new ChatResponse
                    {
                        ConfirmationMessage = response.Reply,
                        UserIssueJson = DeserializeJson(state),
                    };

                    return Ok(responseMessage);
                }

                if (!state.PreferredSlotIndex.HasValue &&
                !string.IsNullOrEmpty(state.IssueType) &&
                !string.IsNullOrEmpty(state.Priority) &&
                !string.IsNullOrEmpty(state.ContactEmail))
                {
                    var slots = await _calendar.GetFreeSlotsAsync(state.Priority);
                    state.AvailableSlots = slots;
                    await _redis.SaveStateAsync(request.UserEmail, state);

                    var slotOptions = string.Join("\n", slots.Select((s, i) =>
                        $"{i + 1}. {s.Start:f} - Tenant: {s.TenantEmail}"));

                    var message = new CRMPropertySuiteUserChatMessages
                    {
                        ConversationId = request.ConversationId,
                        MessageId = Guid.NewGuid(),
                        MessageText = $"{response.Reply}:\n{slotOptions}",
                        Sender = "assistant",
                        SentAt = DateTime.Now
                    };

                    await _chatDetails.AddMessages(new List<CRMPropertySuiteUserChatMessages> { message });

                    var responseMessage = new ChatResponse
                    {
                        ConfirmationMessage = $"{response.Reply}:\n{slotOptions}",
                        UserIssueJson = DeserializeJson(state),
                    };

                    return Ok(responseMessage);
                }

                var finalMessage = new CRMPropertySuiteUserChatMessages
                {
                    ConversationId = request.ConversationId,
                    MessageId = Guid.NewGuid(),
                    MessageText = response.Reply,
                    Sender = "assistant",
                    SentAt = DateTime.Now
                };

                await _chatDetails.AddMessages(new List<CRMPropertySuiteUserChatMessages> { finalMessage });


                var finalResponseMessage = new ChatResponse
                {
                    ConfirmationMessage = response.Reply,
                    UserIssueJson = DeserializeJson(state),
                };

                return Ok(finalResponseMessage);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        [HttpGet("redis-test")]
        public async Task<IActionResult> TestRedis([FromServices] IConnectionMultiplexer redis)
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync("ping", "pong");
            var result = await db.StringGetAsync("ping");
            return Ok(result);
        }

        private ExtractionResult DeserializeJson(ChatState state)
        {
            var json = JsonSerializer.Serialize<ChatState>(state, new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true });
            var jsonDesrialized = JsonSerializer.Deserialize<ExtractionResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return jsonDesrialized!;
        }

        private void SendPushnotification(FCMPushNotification pushNotification)
        {
            _pushNotificationService.SendPushNotification(pushNotification);
        }
    }
}
