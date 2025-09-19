using Microsoft.Extensions.Logging;
using NVRNotifier.Core.Clients;
using NVRNotifier.Core.Models;
using NVRNotifier.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace NVRNotifier.Bot.Services
{
    public class UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, ZmWsClientFactory zmWsClientFactory, IAppSettings appSettings) : IUpdateHandler
    {
        private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];

        private ZmWsClient zmWsClient = zmWsClientFactory.Create();
        private EventHandler<AlarmReceivedMessage?>? onMessageReceivedHandler = null;
        private EventHandler<string?>? onErrorHandler = null;

        private bool isTurnedOn = false;

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            logger.LogInformation("HandleError: {Exception}", exception);
            // Cooldown in case of network connection error
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } when message.Chat.Id != appSettings.ChatId => Task.CompletedTask, // Игнорируем сообщения из других чатов
                { Message: { } message } => OnMessage(message),
                //{ EditedMessage: { } message } => OnMessage(message),
                //{ CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
                //{ InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
                //{ ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
                //{ Poll: { } poll } => OnPoll(poll),
                //{ PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        private async Task OnMessage(Message msg)
        {
            if (msg.Text == null || msg.Text[0] != '/')
                return;

            logger.LogInformation($"Получена команда: {msg.Text}");

            var botUsername = (await bot.GetMe()).Username;

            await (msg.Text.Split(' ')[0] switch
            {
                var command when command == "/on" || command == $"/on@{botUsername}" => TurnOnAlarm(msg),
                var command when command == "/off" || command == $"/off@{botUsername}" => TurnOffAlarm(msg),
                var command when command == "/status" || command == $"/status@{botUsername}" => GetStatus(msg),
                var command when command == "/help" || command == $"/help@{botUsername}" => Usage(msg),
                _ => Usage(msg)
            });
        }

        private async Task TurnOnAlarm(Message msg)
        {
            try
            {
                if (isTurnedOn)
                {
                    await GetStatus(msg);
                    return;
                }

                onErrorHandler = (sender, errorMessage) =>
                {
                    bot.SendMessage(msg.Chat, "❌Ошибка сервиса ZoneMinder❌", ParseMode.Html);
                    throw new Exception($"{errorMessage}");
                };
                zmWsClient.OnError += onErrorHandler;

                onMessageReceivedHandler = async (sender, zmMessage) =>
                {
                    var cameraName = zmMessage?.Events[0].Name;
                    var eventId = zmMessage?.Events[0].EventId;
                    var cause = zmMessage?.Events[0].Cause ?? string.Empty;

                    if (cause == "Motion") // Начало события
                    {
                        await bot.SendMessage(msg.Chat, "Обнаружено движение...", ParseMode.Html);
                    }
                    else if (cause.Contains("End:Motion")) // Конец события
                    {
                        var pathToVideo = $"{appSettings.ZoneMinderVideoPath}/{cameraName}/{DateTime.Today.ToString("O")}/{eventId}/{eventId}-video.mp4";
#if DEBUG
                        pathToVideo = @"C:\Users\nikka\Downloads\106-video.mp4";
#endif
                        await using FileStream stream = File.OpenRead(pathToVideo);
                        await bot.SendVideo(msg.Chat, stream);
                        //await bot.SendPhoto(msg.Chat, $"https://{appSettings.ZoneMinderHost}/zm/index.php?view=image&eid={eventId}&fid=objdetect&width=600", $"{cameraName}");
                    }

                };
                zmWsClient.OnEventReceived += onMessageReceivedHandler;

                await zmWsClient.ConnectAsync();

                isTurnedOn = true;

                await bot.SendMessage(msg.Chat, "▶<b>Включено</b> оповещение с камер", ParseMode.Html);
                logger.LogInformation("Включено оповещение с камер");

            }
            catch(Exception ex)
            {
                logger.LogError($"Ошибка при попытке запуска получения событий: {ex.Message}");
            }
        }

        private async Task TurnOffAlarm(Message msg)
        {
            if (!isTurnedOn)
            {
                await GetStatus(msg);
                return;
            }

            isTurnedOn = false;

            if (onMessageReceivedHandler != null)
            {
                zmWsClient.OnEventReceived -= onMessageReceivedHandler;
                zmWsClient.OnError -= onErrorHandler;
                onMessageReceivedHandler = null;
            }

            await zmWsClient.DisconnectAsync();

            await bot.SendMessage(msg.Chat, $"⏸<b>Выключено</b> оповещение с камер", ParseMode.Html);
            logger.LogInformation("Выключено оповещение с камер");
        }

        private async Task GetStatus(Message msg)
        {
            if (isTurnedOn)
            {
                await bot.SendMessage(msg.Chat, "Статус: \U0001f7e2Работает", ParseMode.Html);
            }
            else
            {
                await bot.SendMessage(msg.Chat, "Статус: 🔴Выключен", ParseMode.Html);
            }
        }

        async Task<Message> Usage(Message msg)
        {
            const string usage = """
                <b><u>Команды бота</u></b>:
                /on             - Включить оповещение с камер
                /off            - Выключить оповещение с камер
                /status         - Проверить состояние работы
            """;
            return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
        //async Task<Message> SendPhoto(Message msg)
        //{
        //    await bot.SendChatAction(msg.Chat, ChatAction.UploadPhoto);
        //    await Task.Delay(2000); // simulate a long task
        //    await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        //    return await bot.SendPhoto(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
        //}

        //// Send inline keyboard. You can process responses in OnCallbackQuery handler
        //async Task<Message> SendInlineKeyboard(Message msg)
        //{
        //    var inlineMarkup = new InlineKeyboardMarkup()
        //        .AddNewRow("1.1", "1.2", "1.3")
        //        .AddNewRow()
        //            .AddButton("WithCallbackData", "CallbackData")
        //            .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        //    return await bot.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
        //}

        //async Task<Message> SendReplyKeyboard(Message msg)
        //{
        //    var replyMarkup = new ReplyKeyboardMarkup(true)
        //        .AddNewRow("1.1", "1.2", "1.3")
        //        .AddNewRow().AddButton("2.1").AddButton("2.2");
        //    return await bot.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
        //}

        //async Task<Message> RemoveKeyboard(Message msg)
        //{
        //    return await bot.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
        //}

        //async Task<Message> RequestContactAndLocation(Message msg)
        //{
        //    var replyMarkup = new ReplyKeyboardMarkup(true)
        //        .AddButton(KeyboardButton.WithRequestLocation("Location"))
        //        .AddButton(KeyboardButton.WithRequestContact("Contact"));
        //    return await bot.SendMessage(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
        //}

        //async Task<Message> StartInlineQuery(Message msg)
        //{
        //    var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        //    return await bot.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
        //        "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
        //}

        //async Task<Message> SendPoll(Message msg)
        //{
        //    return await bot.SendPoll(msg.Chat, "Question", PollOptions, isAnonymous: false);
        //}

        //async Task<Message> SendAnonymousPoll(Message msg)
        //{
        //    return await bot.SendPoll(chatId: msg.Chat, "Question", PollOptions);
        //}

        //static Task<Message> FailingHandler(Message msg)
        //{
        //    throw new NotImplementedException("FailingHandler");
        //}

        //// Process Inline Keyboard callback data
        //private async Task OnCallbackQuery(CallbackQuery callbackQuery)
        //{
        //    logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        //    await bot.AnswerCallbackQuery(callbackQuery.Id, $"Received {callbackQuery.Data}");
        //    await bot.SendMessage(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
        //}

        //#region Inline Mode

        //private async Task OnInlineQuery(InlineQuery inlineQuery)
        //{
        //    logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        //    InlineQueryResult[] results = [ // displayed result
        //        new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
        //    new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        //    ];
        //    await bot.AnswerInlineQuery(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
        //}

        //private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
        //{
        //    logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        //    await bot.SendMessage(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
        //}

        //#endregion

        //private Task OnPoll(Poll poll)
        //{
        //    logger.LogInformation("Received Poll info: {Question}", poll.Question);
        //    return Task.CompletedTask;
        //}

        //private async Task OnPollAnswer(PollAnswer pollAnswer)
        //{
        //    var answer = pollAnswer.OptionIds.FirstOrDefault();
        //    var selectedOption = PollOptions[answer];
        //    if (pollAnswer.User != null)
        //        await bot.SendMessage(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
        //}

    }
}
