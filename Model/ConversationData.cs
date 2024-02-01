using System;
using System.Collections.Generic;

namespace GenAiBot.Model
{
  public class ConversationData {
    public List<Message> Messages { get; set; } = new List<Message>();

    public void Add(Message message) {
      Messages.Add(message);
    }

    public void Clear() {
      Messages.Clear();
    }

    public int getMessageCount() {
      return Messages.Count;
    }

    // TODO Add the rotation of the history

    public string getHistory() {
      string history = "";
      foreach (Message message in Messages) {
        history += String.Format("{0}: {1}\n", getMessageType(message.Type), message.Text);
      }
      return history;
    }

    private string getMessageType(MessageType messageType) {
      return messageType == MessageType.CHATBOT ? "ChatBot" : "User";
    }
  }
}