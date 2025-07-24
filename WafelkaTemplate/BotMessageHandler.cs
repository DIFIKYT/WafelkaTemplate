using Telegram.Bot;
using Telegram.Bot.Types;

namespace WafelkaTemplate
{
    public class BotMessageHandler
    {
        private readonly TelegramBotClient _bot;
        private readonly GoogleSheetsHelper _sheetsHelper;
        private readonly Random _random = new();
        private CancellationToken _cancellationToken;

        private readonly string _emptyData;
        private readonly string _atSign;
        private readonly string _hashtag;
        private readonly string _hashtagRequest;
        private readonly string _hashtagPayment;
        private readonly string _hashtagPaid;
        private readonly string _hashtagFeedback;
        private readonly string _hashtagRejection;
        private readonly string _wafelkaCall;

        private readonly string _responseToIncorrectAnswer;
        private readonly string _responseToEmptyAnswer;
        private readonly string _responseToAnswerToPhoto;
        private readonly string _responseToWrongHashtag;
        private readonly string _responseToNotFoundList;

        private readonly List<string> _hashtags = [];
        private readonly List<string> _responsesToCallWafelka = [];
        private readonly List<string> _responsesToRequest = [];
        private readonly List<string> _responsesToPayment = [];
        private readonly List<string> _responsesToPaid = [];
        private readonly List<string> _responsesToFeedback = [];
        private readonly List<string> _responsesToRejection = [];

        public BotMessageHandler(TelegramBotClient bot, GoogleSheetsHelper sheetsHelper, BotConfig config)
        {
            _bot = bot;
            _sheetsHelper = sheetsHelper;

            _emptyData = config.EmptyData!;
            _atSign = config.AtSign!;
            _hashtag = config.Hashtag!;
            _hashtagRequest = config.HashtagRequest!;
            _hashtagPayment = config.HashtagPayment!;
            _hashtagPaid = config.HashtagPaid!;
            _hashtagFeedback = config.HashtagFeedback!;
            _hashtagRejection = config.HashtagRejection!;
            _wafelkaCall = config.WafelkaCall!;

            _responseToIncorrectAnswer = config.ResponseToIncorrectAnswer!;
            _responseToEmptyAnswer = config.ResponseToEmptyAnswer!;
            _responseToAnswerToPhoto = config.ResponseToAnswerToPhoto!;
            _responseToWrongHashtag = config.ResponseToWrongHashtag!;
            _responseToNotFoundList = config.ResponseToNotFoundList!;

            _responsesToCallWafelka = config.ResponsesToCallWafelka!.ToList();
            _responsesToRequest = config.ResponsesToRequest!.ToList();
            _responsesToPayment = config.ResponsesToPayment!.ToList();
            _responsesToPaid = config.ResponsesToPaid!.ToList();
            _responsesToFeedback = config.ResponsesToFeedback!.ToList();
            _responsesToRejection = config.ResponsesToRejection!.ToList();

            _hashtags.Add(_hashtagRequest);
            _hashtags.Add(_hashtagPayment);
            _hashtags.Add(_hashtagPaid);
            _hashtags.Add(_hashtagFeedback);
            _hashtags.Add(_hashtagRejection);
        }

        public async Task MessageHandleAsync(Message message, CancellationToken cancellationToken)
        {
            if (message.Text == null || message.From == null || message.From.Username == null)
                return;

            string[] lines;
            string nickname = _atSign + message.From.Username;

            if (message.Text == _wafelkaCall)
            {
                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"{nickname}, {_responsesToCallWafelka[_random.Next(_responsesToCallWafelka.Count)]}",
                    cancellationToken: _cancellationToken,
                    replyParameters: message
                );
            }

            _cancellationToken = cancellationToken;

            if (message.Text.StartsWith(_hashtag))
            {
                lines = message.Text.Split("\n");

                if (message.Text.StartsWith(_hashtagRequest))
                {
                    await AddRequest(message, lines, nickname);

                    return;
                }

                if (_hashtags.Contains(lines[0]) == false)
                {
                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: _responseToWrongHashtag,
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );

                    return;
                }

                if (message.ReplyToMessage == null)
                {
                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: _responseToEmptyAnswer,
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );

                    return;
                }

                if (message.ReplyToMessage.Photo != null)
                {
                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: _responseToAnswerToPhoto,
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );
                }
                else if (message.ReplyToMessage.Text != null)
                {
                    if (message.ReplyToMessage.Text.StartsWith(_hashtagRequest))
                    {
                        string? firstLine = lines.Length > 0 ? lines[0] : null;
                        string firstLineOfReply = message.ReplyToMessage.Text.Split("\n")[0];

                        if (firstLine == _hashtagPayment)
                        {
                            await AddPayment(lines, message, firstLineOfReply);
                        }
                        else if (firstLine == _hashtagPaid)
                        {
                            await AddPaid(message, firstLineOfReply);
                        }
                        else if (firstLine == _hashtagFeedback)
                        {
                            await AddFeedback(lines, message, firstLineOfReply);
                        }
                        else if (firstLine == _hashtagRejection)
                        {
                            await Refuse(message, firstLineOfReply);
                        }
                    }
                    else
                    {
                        await _bot.SendMessage(
                            chatId: message.Chat.Id,
                            text: _responseToIncorrectAnswer,
                            cancellationToken: _cancellationToken,
                            replyParameters: message
                        );
                    }
                }
            }
        }

        public void SubscribeOnSheetsHelper()
        {
            _sheetsHelper.ListNotFound += OnListNotFound;
        }

        private async Task AddRequest(Message message, string[] lines, string nickname)
        {
            string applicationsListName;
            string fullName;
            string paymentDetails;
            string adDate;
            string size;
            string socialLink;
            string buyoutPrice;
            string paymentPrice;
            string articleNumber;

            applicationsListName = lines[0].Substring(lines[0].IndexOf("_") + 1);

            if (_sheetsHelper.TryGetList(applicationsListName, message) == false)
                return;

            fullName = lines.Length > 1 ? lines[1] : _emptyData;
            buyoutPrice = lines.Length > 2 ? lines[2] : _emptyData;
            paymentPrice = lines.Length > 3 ? lines[3] : _emptyData;
            paymentDetails = lines.Length > 4 ? lines[4] : _emptyData;
            adDate = lines.Length > 5 ? lines[5] : _emptyData;
            size = lines.Length > 6 ? lines[6] : _emptyData;
            socialLink = lines.Length > 7 ? lines[7] : _emptyData;
            articleNumber = lines.Length > 8 ? lines[8] : _emptyData;

            await _sheetsHelper.AddRequestAsync(
                applicationsListName,
                nickname,
                fullName,
                paymentDetails,
                adDate,
                size,
                socialLink,
                buyoutPrice,
                paymentPrice,
                Convert.ToString(message.MessageId),
                articleNumber
            );

            await _bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"{_responsesToRequest[_random.Next(_responsesToRequest.Count)]}",
                cancellationToken: _cancellationToken,
                replyParameters: message
            );
        }

        private async Task AddPayment(string[] lines, Message message, string firstLineOfReply)
        {
            string receivedDate = lines.Length > 1 ? lines[1] : _emptyData;
            string orderNumber = lines.Length > 2 ? lines[2] : _emptyData;
            string applicationsListName = firstLineOfReply.Substring(firstLineOfReply.IndexOf("_") + 1);

            if (_sheetsHelper.TryGetList(applicationsListName!, message))
            {
                if (FindRequest(message, applicationsListName!, out int rowNumber))
                {
                    await _sheetsHelper.AddPaymentDataAsync(applicationsListName!, rowNumber, receivedDate, orderNumber);

                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"{_responsesToPayment[_random.Next(_responsesToPayment.Count)]}",
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );
                }
            }
        }

        private async Task AddPaid(Message message, string firstLineOfReply)
        {
            string applicationsListName = firstLineOfReply.Substring(firstLineOfReply.IndexOf("_") + 1);

            if (_sheetsHelper.TryGetList(applicationsListName!, message))
            {
                if (FindRequest(message, applicationsListName!, out int rowNumber))
                {
                    await _sheetsHelper.UpdateStatus(applicationsListName!, rowNumber);

                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"{_responsesToPaid[_random.Next(_responsesToPaid.Count)]}",
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );
                }
            }
        }

        private async Task AddFeedback(string[] lines, Message message, string firstLineOfReply)
        {
            string availabilityStatus = lines.Length > 1 ? lines[1] : _emptyData;
            string link = lines.Length > 2 ? lines[2] : _emptyData;
            string applicationsListName = firstLineOfReply.Substring(firstLineOfReply.IndexOf("_") + 1); ;

            if (_sheetsHelper.TryGetList(applicationsListName!, message))
            {
                if (FindRequest(message, applicationsListName!, out int rowNumber))
                {
                    await _sheetsHelper.AddFeedBackDataAsync(applicationsListName!, rowNumber, availabilityStatus, link);

                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"{_responsesToFeedback[_random.Next(_responsesToFeedback.Count)]}",
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );
                }
            }
        }

        private async Task Refuse(Message message, string firstLineOfReply)
        {
            string applicationsListName = firstLineOfReply.Substring(firstLineOfReply.IndexOf("_") + 1);

            if (_sheetsHelper.TryGetList(applicationsListName!, message))
            {
                if (FindRequest(message, applicationsListName!, out int rowNumber))
                {
                    _sheetsHelper.DeleteRow(applicationsListName!, rowNumber);

                    await _bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"{_responsesToRejection[_random.Next(_responsesToRejection.Count)]}",
                        cancellationToken: _cancellationToken,
                        replyParameters: message
                    );
                }
            }
        }

        private bool FindRequest(Message message, string listName, out int rowNumber)
        {
            if (message == null || message.ReplyToMessage == null)
            {
                rowNumber = -1;
                return false;
            }

            rowNumber = _sheetsHelper.GetRowByMessageId(listName, Convert.ToString(message.ReplyToMessage.MessageId));

            return rowNumber != -1;
        }

        private void OnListNotFound(Message message)
        {
            if (message.From == null)
                return;

            _bot.SendMessage(
                chatId: message.Chat.Id,
                text: _responseToNotFoundList,
                cancellationToken: _cancellationToken,
                replyParameters: message
            );
        }
    }
}