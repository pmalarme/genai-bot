// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with EchoBot .NET Template version v4.17.1

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using GenAiBot.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.VisualBasic;

namespace GenAiBot.Bots
{
    public class GenAiBot : ActivityHandler
    {

        private readonly IConfiguration _configuration;
        private readonly string _prompt;
        private readonly Kernel _kernel;
        private readonly KernelFunction _chatFunction;
        private readonly ConversationState _conversationState;

        public GenAiBot(IConfiguration configuration, ConversationState conversationState)
        {
            _configuration = configuration;
            _prompt = @"
            As an advance chatbot named Kyle, your primary goal is to assist users to the best of your availability. This may involve answering questions, providing helpful information, or completing tasks based on user input. In order to effectively assist users, it is important to be detailed and thorough in your responses. Use examples and evidence to support your points and justify your recommendations or solutions.
            
            {{$history}}
            User: {{$input}}
            ChatBot:";
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                _configuration["AzureOpenAI:DeploymentId"],
                _configuration["AzureOpenAI:Endpoint"],
                _configuration["AzureOpenAI:ApiKey"]
            );
            _kernel = builder.Build();
            _chatFunction = _kernel.CreateFunctionFromPrompt(_prompt, executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 1000 });
            _conversationState = conversationState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello I'm Kyle, how can I help you?";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                    var conversationData = GetConversationData(turnContext);
                    Console.WriteLine("===================================\nConversation Data\n===================================\n");
                    // As now it is in memory, the storage is no kept between sessions because the bot is restarted.
                    // TODO define how to manage the sate and how to fix a limit to the amount of messages
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get conversation data
            var conversationData = GetConversationData(turnContext);
            
            // Create prompt
            var inputText = turnContext.Activity.Text;
            var history = conversationData.getHistory();
            var result = await _kernel.InvokeAsync(_chatFunction, new() { ["input"] = inputText, ["history"] = history });
            var resultString =  $"{result}";

            // Update conversation data
            conversationData.Add(new Message { Text = inputText, Type = MessageType.USER });
            conversationData.Add(new Message { Text = resultString, Type = MessageType.CHATBOT });

            await turnContext.SendActivityAsync(MessageFactory.Text(resultString, resultString), cancellationToken);
        }

        private ConversationData GetConversationData(ITurnContext turnContext)
        {
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            return conversationStateAccessors.GetAsync(turnContext, () => new ConversationData()).Result;
        }
        
    }
}
