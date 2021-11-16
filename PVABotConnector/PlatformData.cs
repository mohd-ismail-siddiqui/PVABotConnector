using System.Collections.Generic;

namespace EP.NETCore.BOT.Data
{
    public class UtteranceRequest
    {
        public UtteranceReqType Type { get; set; }
        public string SessionId { get; set; }
        public string BotId { get; set; }
        public string ChannelId { get; set; }
        public bool IsVoiceMode { get; set; }
        public bool IsAnonymous { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Utterance { get; set; }
        public string DataJson { get; set; }
        public List<AttachmentInfo> Attachments { get; set; }
    }

    public class AttachmentInfo
    {
        public string ContentType { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class UtteranceResponse
    {
        public bool EndConversation { get; set; }
        public List<UtteranceResMessage> Messages { get; set; }
    }

    public class UtteranceResMessage
    {
        public int Sequence { get; set; }
        public bool IsCardContent { get; set; }
        public string Content { get; set; }
        public string VoiceContent { get; set; }
        public string AltContent { get; set; }
        public CardType CardType { get; set; }
        public List<string> Prompts { get; set; }
    }

    public enum CardType
    {
        Adaptive = 0,
        Html = 1,
        Document = 2,
        InlinePdf = 3,
        StaticCarousel = 4,
        DynamicCarousel = 5
    }

    public enum UtteranceReqType
    {
        Init = 0,
        Message = 1,
        Data = 2,
        Close = 3,
        Attachment = 4
    }
}
