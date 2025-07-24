using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Telegram.Bot.Types;
using WafelkaTemplate;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace WafelkaTemplate
{
    public class GoogleSheetsHelper
    {
        private enum Column
        {
            A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q
        }

        private const int FirstDataRowNumber = 3;
        private static readonly string[] _scopes = [SheetsService.Scope.Spreadsheets];
        private readonly string _orderStatus;
        private readonly string _paidStatus;
        private readonly string _paymentsListName;
        private readonly string _summaryListName;
        private readonly SheetsService _service;
        private readonly string _spreadsheetId;
        private readonly DailyMidnightTaskScheduler _timer;

        public event Action<Message>? ListNotFound;

        public GoogleSheetsHelper(BotConfig config, DailyMidnightTaskScheduler timer)
        {
            GoogleCredential credential = GoogleCredential.FromFile(config.CredentialsPath).CreateScoped(_scopes);
            _service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });
            _spreadsheetId = config.SpreadsheetId!;

            _paymentsListName = config.PaymentsListName!;
            _summaryListName = config.SummaryListName!;

            _orderStatus = config.OrderStatus!;
            _paidStatus = config.PaidStatus!;

            _timer = timer;

            _timer.TaskExecuted += OnTaskExecuted;
        }

        private async Task OnTaskExecuted()
        {
            await UpdatePaymentsData();
        }

        public async Task AddRequestAsync(string listName, string nickname, string fullName, string paymentDetails, string adDate,
        string size, string socialLink, string buyoutPrice, string paymentPrice, string messageId, string articleNumber)
        {
            ValueRange valueRange = GetValueByShellsRange(_spreadsheetId, $"{listName}!{Column.A}:{Column.A}");

            int lastRow = valueRange.Values == null ? FirstDataRowNumber : valueRange.Values.Count + 1;
            string date = $"{DateTime.Today:dd.MM.yyyy}";
            Dictionary<string, string> dataToUpdate = [];

            Dictionary<Column, string> dataToColumns = new()
            {
                { Column.A, nickname },
                { Column.B, articleNumber },
                { Column.C, fullName },
                { Column.D, _orderStatus },
                { Column.E, buyoutPrice },
                { Column.F, date },
                { Column.G, paymentPrice },
                { Column.H, paymentDetails },
                { Column.I, socialLink },
                { Column.J, adDate },
                { Column.M, size },
                { Column.P, messageId }
            };

            foreach (Column key in dataToColumns.Keys)
            {
                dataToUpdate[$"{listName}!{key}{lastRow}"] = dataToColumns[key];
            }

            await BatchUpdateValues(dataToUpdate, _spreadsheetId);
        }

        public async Task AddPaymentDataAsync(string listName, int rowNumber, string receivedDate, string orderNumber)
        {
            Dictionary<string, string> dataToUpdate = [];

            Dictionary<Column, string> dataToColumns = new()
            {
                { Column.K, receivedDate },
                { Column.Q, orderNumber }
            };

            foreach (Column key in dataToColumns.Keys)
            {
                dataToUpdate[$"{listName}!{key}{rowNumber}"] = dataToColumns[key];
            }

            await BatchUpdateValues(dataToUpdate, _spreadsheetId);
        }

        public async Task AddFeedBackDataAsync(string listName, int rowNumber, string availabilityStatus, string link)
        {
            Dictionary<string, string> dataToUpdate = [];

            Dictionary<Column, string> dataToColumns = new()
            {
                { Column.O, availabilityStatus },
                { Column.N, link }
            };

            foreach (Column key in dataToColumns.Keys)
            {
                dataToUpdate[$"{listName}!{key}{rowNumber}"] = dataToColumns[key];
            }

            await BatchUpdateValues(dataToUpdate, _spreadsheetId);
        }

        public async Task UpdateStatus(string listName, int rowNumber)
        {
            Dictionary<string, string> dataToUpdate = [];

            string date = $"{DateTime.Today:dd.MM.yyyy}";

            Dictionary<Column, string> dataToColumns = new()
            {
                { Column.D, _paidStatus },
                { Column.L, date }
            };

            foreach (Column key in dataToColumns.Keys)
            {
                dataToUpdate[$"{listName}!{key}{rowNumber}"] = dataToColumns[key];
            }

            await BatchUpdateValues(dataToUpdate, _spreadsheetId);
        }

        public void DeleteRow(string listName, int rowNumber)
        {
            string range = $"{listName}!{Column.A}{rowNumber}:{Column.Q}{rowNumber}";
            ClearRequest request = _service.Spreadsheets.Values.Clear(new ClearValuesRequest(), _spreadsheetId, range);
            request.Execute();
        }

        public bool TryGetList(string listName, Message message)
        {
            if (listName == null)
                return false;

            Spreadsheet spreadsheet = _service.Spreadsheets.Get(_spreadsheetId).Execute();

            foreach (Sheet sheet in spreadsheet.Sheets)
            {
                if (sheet.Properties.Title == listName)
                {
                    return true;
                }
            }

            ListNotFound?.Invoke(message);
            return false;
        }

        public int GetRowByMessageId(string listName, string messageId)
        {
            string range = $"{listName}!{Column.P}:{Column.P}";
            ValueRange response = _service.Spreadsheets.Values.Get(_spreadsheetId, range).Execute();

            if (response.Values != null)
            {
                for (int i = 0; i < response.Values.Count; i++)
                {
                    if (response.Values[i].Count > 0 && response.Values[i][0].ToString() == messageId)
                    {
                        return i + 1;
                    }
                }
            }

            return -1;
        }

        public async Task UpdatePaymentsData()
        {
            try
            {
                Spreadsheet spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                Dictionary<string, string> dataToUpdate = [];
                string todayDate = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                int rowNumber = -1;
                int paymentsSum = 0;

                foreach (Sheet sheet in spreadsheet.Sheets)
                {
                    if (sheet.Properties.Title == _paymentsListName || sheet.Properties.Title == _summaryListName)
                    {
                        continue;
                    }

                    ValueRange paymentSheetDateValues = GetValueByShellsRange(_spreadsheetId, $"{_paymentsListName}!{Column.A}:{Column.A}");
                    ValueRange dateValues = GetValueByShellsRange(_spreadsheetId, $"{sheet.Properties.Title}!{Column.L}:{Column.L}");
                    ValueRange paymentsValues = GetValueByShellsRange(_spreadsheetId, $"{sheet.Properties.Title}!{Column.G}:{Column.G}");

                    foreach (IList<object> row in dateValues.Values)
                    {
                        foreach (object value in row)
                        {
                            if (value.ToString() == todayDate)
                            {
                                if (int.TryParse(paymentsValues.Values[dateValues.Values.IndexOf(row)][0].ToString(), out int paymentsValue))
                                {
                                    paymentsSum += paymentsValue;
                                }
                            }
                        }
                    }

                    foreach (IList<object> row in paymentSheetDateValues.Values)
                    {
                        foreach (object value in row)
                        {
                            if (value.ToString() == todayDate)
                            {
                                rowNumber = paymentSheetDateValues.Values.IndexOf(row) + 1;
                                break;
                            }
                        }
                    }

                    if (rowNumber == -1)
                    {
                        rowNumber = paymentSheetDateValues.Values.Count + 1;

                        dataToUpdate[$"{_paymentsListName}!{Column.A}{rowNumber}"] = todayDate;
                        dataToUpdate[$"{_paymentsListName}!{Column.B}{rowNumber}"] = paymentsSum.ToString();
                    }
                    else
                    {
                        dataToUpdate[$"{_paymentsListName}!{Column.B}{rowNumber}"] = paymentsSum.ToString();
                    }

                    await Task.Delay(5000);
                }

                await BatchUpdateValues(dataToUpdate, _spreadsheetId);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Ошибка в сборе оплат: " + exception);
            }
        }

        private async Task BatchUpdateValues(Dictionary<string, string> dataToUpdate, string spreadsheetId)
        {
            List<ValueRange> valueRanges = [];

            foreach (KeyValuePair<string, string> kvp in dataToUpdate)
            {
                string range = kvp.Key;
                valueRanges.Add(new ValueRange { Range = range, Values = [[kvp.Value]] });
            }

            BatchUpdateValuesRequest batchRequest = new()
            {
                ValueInputOption = "USER_ENTERED",
                Data = valueRanges,
            };

            try
            {
                BatchUpdateRequest request = _service.Spreadsheets.Values.BatchUpdate(batchRequest, spreadsheetId);
                await request.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch update: {ex.Message}");
            }
        }

        private ValueRange GetValueByShellsRange(string spreadsheetId, string range)
        {
            return _service.Spreadsheets.Values.Get(spreadsheetId, range).Execute();
        }
    }
}