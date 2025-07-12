using Azure;
using Azure.AI.OpenAI;
using System.Text.Json;
using OpenAI.Chat;
using SmartPropertySuite.Models;
using SmartPropertySuite.IServices;

namespace SmartPropertySuite.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatClient chatClient;
        private readonly string _deployment;

        public ChatService(IConfiguration config)
        {
            string apiKey = config["AzureConfiguration:ApiKey"];
            string endpoint = config["AzureConfiguration:Endpoint"];
            _deployment = config["AzureConfiguration:Deployment"];
            var credentials = new AzureKeyCredential(apiKey);

            AzureOpenAIClient azureClient = new AzureOpenAIClient(new Uri(endpoint), credentials);
            chatClient = azureClient.GetChatClient(_deployment);
        }

        public async Task<string> GetBotReplyAsync(string input, string systemPrompt)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(input)
                };

                var chatOptions = new ChatCompletionOptions
                {
                    Temperature = 0.7f
                };

                var response = await chatClient.CompleteChatAsync(messages, chatOptions);

                return response?.Value.Content?.FirstOrDefault().Text.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<ExtractionResult> ExtractUserDetailsAsync(string input)
        {
            try
            {
                var propmt = @"
                You are an assistant for a property management bot. Extract the following structured data from the user input:
                    - IssueType (like plumbing, electrical, etc.)
                    - Priority (low, medium, high)
                    - ContactEmail
                    - PreferredSlotIndex (if the user chose a time slot, extract the index like 1, 2, 3... 
                        based on their response. Accept both:
                        - Numerical values like ""1"", ""2"", ""3"", etc.
                        - Ordinal phrases like ""first one"", ""second slot"", ""middle one"", ""last"", etc.)
                    
                    There are always 5 available time slots. So:
                    - 'first'means 1
                    - 'second' means 2
                    - 'middle one' or 'third' means 3
                    - 'fourth' means 4
                    - 'last', 'fifth', or 'final one' means 5

                    You may also use your general knowledge of ordinal terms and user intent to determine the most likely slot index (from 1 to 5) if it is clearly implied.

                    - Only infer Priority if the user's message **clearly implies urgency**. Examples: words like ""urgent"", ""emergency"", ""immediate"", ""critical"", ""not important"". These are just examples you can use your judgment to identify urgency cues in the user's message. Use your best judgment to choose one of:
                      ""low"", ""medium"", or ""high""
                    - If no such cues are found, leave Priority as null.

                    Do not add a single keyword or word just respond ONLY with the extracted values in JSON format like:
                    {
                        ""IssueType"": ""plumbing"",
                        ""Priority"": ""high"",
                        ""ContactEmail"": ""xyz@example.com"",
                        ""PreferredSlotIndex"": 1
                    }

                    Do NOT add explanations. Leave fields null if not mentioned.";

                var response = await GetBotReplyAsync(input, propmt);

                return JsonSerializer.Deserialize<ExtractionResult>(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<string> ExtractIntentAsync(string input)
        {
            var prompt = @"
                You are an assistant for a property management chatbot. Analyze the user message and identify the intent.

                Respond only with one of the following intents:
                - new_request
                - continue_existing
                - greeting
                - unknown

                Examples:
                - ""I want to book another appointment"" → new_request
                - ""Let's continue from before"" → continue_existing
                - ""Hi there"" → new_request
                - ""I'm facing an issue"" → unknown

                Now, what is the user's intent?
                ";

            var reply = await GetBotReplyAsync(input, prompt);
            return reply?.Trim().ToLower();
        }
    }
}
