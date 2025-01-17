﻿using System;
using ClassAssistantBot.Models;
using ClassAssistantBot.Services;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;

namespace ClassAssistantBot.Controllers
{
    public class CallBackController
    {
        private BotClient bot;
        private Message message;
        private CallbackQuery callbackQuery;
        private Telegram.BotAPI.AvailableTypes.User appUser;
        private PendingDataHandler pendingDataHandler;
        private UserDataHandler userDataHandler;
        private TeacherDataHandler teacherDataHandler;
        private StudentDataHandler studentDataHandler;
        private DiaryDataHandler diaryDataHandler;
        private PracticClassDataHandler practicClassDataHandler;
        private GuildDataHandler guildDataHandler;

        public CallBackController(BotClient bot, DataAccess dataAccess)
        {
            this.bot = bot;
            this.message = new Message();
            this.callbackQuery = new CallbackQuery();
            this.appUser = new Telegram.BotAPI.AvailableTypes.User();
            this.pendingDataHandler = new PendingDataHandler(dataAccess);
            this.userDataHandler = new UserDataHandler(dataAccess);
            this.teacherDataHandler = new TeacherDataHandler(dataAccess);
            this.practicClassDataHandler = new PracticClassDataHandler(dataAccess);
            this.studentDataHandler = new StudentDataHandler(dataAccess);
            this.diaryDataHandler = new DiaryDataHandler(dataAccess);
            this.guildDataHandler = new GuildDataHandler(dataAccess);
        }

        public void ProcessCallBackQuery(CallbackQuery callbackQuery)
        {
            this.callbackQuery = callbackQuery;
            this.message = callbackQuery.Message;
            if (callbackQuery.Message.From == null)
            {
                 Logger.Error($"Error: Mensaje con usuario nulo, problemas en el servidor");
                 bot.SendMessage(chatId: message.Chat.Id,
                                text: "Lo siento, estoy teniendo problemas mentales y estoy en una consulta del psiquiátra.");
                return;
            }
            // Ignore user 777000 (Telegram)s
            if (callbackQuery.From.Id == 777000)
            {
                return;
            }
            this.appUser = callbackQuery.From;

            var user =  userDataHandler.GetUser(appUser);

             Logger.Warning($"New message from chat id: {(string.IsNullOrEmpty(user.Username) ? user.ChatId : user.Username)}");
             Logger.Warning($"Message Text: {callbackQuery.Data}");

            if (callbackQuery.Data.Contains("NextPending//"))
            {
                var data = callbackQuery.Data.Split("//");
                int page = int.Parse(data[1]);
                var interactionType = (Models.InteractionType)int.Parse(data[2]);
                var pendings =  pendingDataHandler.GetPendings(user, false, interactionType, page);
                 Menu.PendingsPaginators(bot, message, pendings.Item1, pendings.Item2, page, interactionType);

            }
            else if (callbackQuery.Data.Contains("BackPending//"))
            {
                var data = callbackQuery.Data.Split("//");
                int page = int.Parse(data[1]);
                var interactionType = (Models.InteractionType)int.Parse(data[2]);
                var pendings =  pendingDataHandler.GetPendings(user, false, interactionType, page);
                 Menu.PendingsPaginators(bot, message, pendings.Item1, pendings.Item2, page, interactionType);
            }
            else if (callbackQuery.Data == "AcceptDiaryUpdate")
            {
                string code = "";
                if (!string.IsNullOrEmpty(callbackQuery.Message.Text))
                {
                    code = callbackQuery.Message.Text.Split(" /")[1];
                }
                else
                    //Error
                    return;
                var pending =  pendingDataHandler.GetPending(code);
                var diary =   diaryDataHandler.GetDiary(pending.ObjectId);
                 bot.SendMessage(chatId: pending.Student.User.ChatId,
                                    text: $"El profesor @{user.Username} ha Aceptado su solicitud de actualización de diario:\n\n{diary.Text}");
                 diaryDataHandler.AcceptDiary(user, pending.Student.UserId, pending.ObjectId);
                var pendings =  pendingDataHandler.GetPendings(user);
                 Menu.PendingsFilters(bot, message, pendings.Item1, pendings.Item2);
            }
            else if(callbackQuery.Data == "DenialOfCreditApplications")
            {
                string code = "";
                bool isText = true;
                if (!string.IsNullOrEmpty(callbackQuery.Message.Text))
                {
                    code = callbackQuery.Message.Text.Split(" /")[1];
                }
                else if (!string.IsNullOrEmpty(callbackQuery.Message.Caption))
                {
                    code = callbackQuery.Message.Caption.Split(" /")[1];
                    isText = false;
                }
                else
                    //Error
                    return;
                var pending =  pendingDataHandler.GetPending(code);
                var imageID = "";
                bool giveMeExplication = false;
                var @object =  pendingDataHandler.GetPendingByCode(code, imageID, giveMeExplication);
                 pendingDataHandler.RemovePending(pending);
                if (isText)
                     bot.SendMessage(chatId: pending.Student.User.ChatId,
                                    text: $"El profesor @{user.Username} ha denegado su solicitud de créditos \n\n{@object.Item1}\n si tienes algún problema pregúntele a él, no la cojas conmigo.");
                else
                     bot.SendPhoto(chatId: pending.Student.User.ChatId,
                                  photo: @object.Item2,
                                  caption: $"El profesor @{user.Username} ha denegado su solicitud de créditos si tienes algún problema pregúntele a él, no la cojas conmigo.");
                var pendings =  pendingDataHandler.GetPendings(user);
                 Menu.PendingsFilters(bot, message, pendings.Item1, pendings.Item2);
            }
            else if(callbackQuery.Data.Contains("GiveMeExplication//"))
            {
                var data = callbackQuery.Data.Split("//");

                var pendingCode = data[1];
                var username = data[2];

                var pending =  pendingDataHandler.GetPending(pendingCode);

                var res =  pendingDataHandler.GetPendingExplicationData(pending, username);

                if (string.IsNullOrEmpty(res.Item2))
                {
                     bot.SendMessage(chatId: pending.Student.User.ChatId,
                                    text: res.Item1);
                }
                else
                {
                     bot.SendPhoto(chatId: pending.Student.User.ChatId,
                                  caption: res.Item1,
                                  photo: res.Item2);
                }
                var pendings =  pendingDataHandler.GetPendings(user);
                 Menu.PendingsFilters(bot, message, pendings.Item1, pendings.Item2);
            }
            else if (callbackQuery.Data.Contains("AssignDirectPending//"))
            {
                var data = callbackQuery.Data.Split("//");
                var command = data[1];
                var teacherUsername = data[2];
                var pending =  pendingDataHandler.GetPending(command);
                if ( teacherDataHandler.ExistTeacher(teacherUsername))
                {
                    var teacherChatId =  pendingDataHandler.AddDirectPending(teacherUsername, pending.Id);
                     bot.SendMessage(chatId: teacherChatId,
                        text: "Le han asignado un pendiente que tiene que revisar.");
                }
                else
                {
                     bot.SendMessage(chatId: user.ChatId,
                        text: $"No existe un usuario con el user name {teacherUsername}.");
                }
                var pendings =  pendingDataHandler.GetPendings(user);
                 Menu.PendingsFilters(bot, message, pendings.Item1, pendings.Item2);
            }
            else if (callbackQuery.Data.Contains("PracticalClassCode//"))
            {
                var data = callbackQuery.Data.Split("//");
                if (user.Status == UserStatus.DeletePracticalClass)
                {
                     practicClassDataHandler.DeletePracticClasses(user, data[1]);
                     Menu.TeacherMenu(bot, message, "Se ha eliminado la Clase Práctica Satisfactoriamente");
                }
                else
                {
                    var students =  studentDataHandler.GetStudents(user);
                     Menu.PracticalClassStudentsList(bot, message, students, data[1], "Seleccione el estudiante:");
                }
            }
            else if (callbackQuery.Data.Contains("StudentUserName//"))
            {
                var data = callbackQuery.Data.Split("//");
                var excercises =  practicClassDataHandler.GetExcercises(user, data[1], data[2]);
                if (excercises.Count() == 0)
                {
                    var students =  studentDataHandler.GetStudents(user);
                     Menu.PracticalClassStudentsList(bot, message, students, data[2], "El estudiante que seleccionó no tiene ejercicios pendientes en esta clase. Seleccione un nuevo estudiante:");
                }
                else
                {
                     Menu.PracticalClassExcersicesList(bot, message, excercises, data[2], data[1], "Seleccione el ejercicio:");
                }
            }
            else if (callbackQuery.Data.Contains("ExcserciseCode//"))
            {
                var data = callbackQuery.Data.Split("//");
                 Menu.PracticalClassIsDouble(bot, message, data[1], data[3], data[2], "Entregó antes de la Clase Práctica?");
            }
            else if (callbackQuery.Data.Contains("IsDouble//"))
            {
                var data = callbackQuery.Data.Split("//");
                var res =  practicClassDataHandler.ReviewPrecticalClass(user, data[1], data[2], data[3], bool.Parse(data[4]));
                if (res.Item1)
                {
                     bot.SendMessage(chatId: res.Item4,
                        text: res.Item2);
                    var excercises =  practicClassDataHandler.GetExcercises(user, data[2], data[3]);
                    if (excercises.Count() == 0)
                    {
                        var students =  studentDataHandler.GetStudents(user);
                         Menu.PracticalClassStudentsList(bot, message, students, data[2], "El estudiante que seleccionó no tiene ejercicios pendientes en esta clase. Seleccione un nuevo estudiante:");
                    }
                    else
                    {
                         Menu.PracticalClassExcersicesList(bot, message, excercises, data[3], data[2], "Seleccione el ejercicio:");
                    }
                }
                else
                {
                    var students =  studentDataHandler.GetStudents(user);
                     Menu.PracticalClassStudentsList(bot, message, students, data[2], $"Ocurrió el siguiente error: {res.Item2}.\n\nVuelva a seleccionar el estudiante:");
                }
            }
            else if (callbackQuery.Data.Contains("SelectGuilds//"))
            {
                var data = callbackQuery.Data.Split("//");
                long guildId = long.Parse(data[1]);
                if (user.Status == UserStatus.DeleteGuild)
                {
                    var students =  guildDataHandler.DeleteGuild(user, guildId);
                     Menu.GuildMenu(bot, message, "Gremio eliminado con éxito");
                    foreach (var student in students.Item2)
                    {
                         bot.SendMessage(chatId: student.User.ChatId,
                        text: $"Ha sido eliminado del Gremio {students.Item1}");
                    }
                }
                else if(user.Status == UserStatus.DetailsGuild)
                {
                    var res =  guildDataHandler.DetailsGuild(user, guildId);
                     Menu.GuildMenu(bot, message, res);
                }
                else if(user.Status == UserStatus.AssignStudentAtGuild)
                {
                    var students =  guildDataHandler.AssignStudentAtGuild(user, guildId);
                     Menu.StudentGuildList(bot, message, students, guildId, "Seleccione el estudiante a agregar al Gremio");
                }
                else if (user.Status == UserStatus.DeleteStudentFromGuild)
                {
                    var students =  guildDataHandler.DeleteStudentFromGuild(user, guildId);
                     Menu.StudentGuildList(bot, message, students, guildId, "Seleccione el estudiante a eliminar del Gremio");
                }
                else if (user.Status == UserStatus.AssignCreditsAtGuild)
                {
                     bot.SendMessage(chatId: user.ChatId,
                        text: $"Envíe un mensaje en el siguiente formato:\n\n<Id del Gremio> <Cantidad de créditos> <Explicación de por qué son los créditos>");
                     bot.SendMessage(chatId: user.ChatId,
                        text: $"Id del Gremio: {guildId}");
                }
            }
            else if (callbackQuery.Data.Contains("SelectStudentOnGuilds//"))
            {
                var data = callbackQuery.Data.Split("//");
                long guildId = long.Parse(data[1]);
                string studentId = data[2];
                if (user.Status == UserStatus.AssignStudentAtGuild)
                {
                    var res =  guildDataHandler.AssignStudentAtGuild(user, guildId, studentId);
                     Menu.GuildMenu(bot, message);
                     bot.SendMessage(chatId: res.Item2,
                        text: $"Ha sido agregado al Gremio {res.Item1}");
                }
                else if (user.Status == UserStatus.DeleteStudentFromGuild)
                {
                    var res =  guildDataHandler.DeleteStudentFromGuild(user, guildId, studentId);
                     Menu.GuildMenu(bot, message);
                     bot.SendMessage(chatId: res.Item2,
                        text: $"Ha sido eliminado del Gremio {res.Item1}");
                }
            }
            else
            {
                Models.InteractionType interactionType = Models.InteractionType.None;
                if (callbackQuery.Data.Contains("Meme"))
                    interactionType = Models.InteractionType.Meme;
                else if (callbackQuery.Data.Contains("ClassTitle"))
                    interactionType = Models.InteractionType.ClassTitle;
                else if (callbackQuery.Data.Contains("ClassIntervention"))
                    interactionType = Models.InteractionType.ClassIntervention;
                else if (callbackQuery.Data.Contains("StatusPhrase"))
                    interactionType = Models.InteractionType.StatusPhrase;
                else if (callbackQuery.Data.Contains("Joke"))
                    interactionType = Models.InteractionType.Joke;
                else if (callbackQuery.Data.Contains("Miscellaneous"))
                    interactionType = Models.InteractionType.Miscellaneous;
                else if (callbackQuery.Data.Contains("Diary"))
                    interactionType = Models.InteractionType.Diary;
                else if (callbackQuery.Data.Contains("RectificationToTheTeacher"))
                    interactionType = Models.InteractionType.RectificationToTheTeacher;
                var pendings =  pendingDataHandler.GetPendings(user, false, interactionType);
                 Menu.PendingsFilters(bot, message, pendings.Item1, pendings.Item2, interactionType);
            }
        }
    }
}

