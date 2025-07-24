using Newtonsoft.Json;
using System.Text.Json;

namespace WafelkaTemplate
{
    public class BotConfig
    {
        [JsonProperty("telegramBotToken")] public string? TelegramBotToken { get; private set; }
        [JsonProperty("credentialsPath")] public string? CredentialsPath { get; private set; }
        [JsonProperty("spreadsheetId")] public string? SpreadsheetId { get; private set; }

        [JsonProperty("paymentsListName")] public string? PaymentsListName { get; private set; }
        [JsonProperty("summaryListName")] public string? SummaryListName { get; private set; }

        [JsonProperty("orderStatus")] public string? OrderStatus { get; private set; }
        [JsonProperty("paidStatus")] public string? PaidStatus { get; private set; }

        [JsonProperty("emptyData")] public string? EmptyData { get; private set; }
        [JsonProperty("atSign")] public string? AtSign { get; private set; }
        [JsonProperty("hashtag")] public string? Hashtag { get; private set; }
        [JsonProperty("hashtagRequest")] public string? HashtagRequest { get; private set; }
        [JsonProperty("hashtagPayment")] public string? HashtagPayment { get; private set; }
        [JsonProperty("hashtagPaid")] public string? HashtagPaid { get; private set; }
        [JsonProperty("hashtagFeedback")] public string? HashtagFeedback { get; private set; }
        [JsonProperty("hashtagRejection")] public string? HashtagRejection { get; private set; }
        [JsonProperty("wafelkaCall")] public string? WafelkaCall { get; private set; }

        [JsonProperty("responsesToCallWafelka")] public string[]? ResponsesToCallWafelka { get; private set; }
        [JsonProperty("responsesToRequest")] public string[]? ResponsesToRequest { get; private set; }
        [JsonProperty("responsesToPayment")] public string[]? ResponsesToPayment { get; private set; }
        [JsonProperty("responsesToPaid")] public string[]? ResponsesToPaid { get; private set; }
        [JsonProperty("responsesToFeedback")] public string[]? ResponsesToFeedback { get; private set; }
        [JsonProperty("responsesToRejection")] public string[]? ResponsesToRejection { get; private set; }

        [JsonProperty("responseToIncorrectAnswer")] public string? ResponseToIncorrectAnswer { get; private set; }
        [JsonProperty("responseToEmptyAnswer")] public string? ResponseToEmptyAnswer { get; private set; }
        [JsonProperty("responseToAnswerToPhoto")] public string? ResponseToAnswerToPhoto { get; private set; }
        [JsonProperty("responseToWrongHashtag")] public string? ResponseToWrongHashtag { get; private set; }
        [JsonProperty("responseToNotFoundList")] public string? ResponseToNotFoundList { get; private set; }
    }
}