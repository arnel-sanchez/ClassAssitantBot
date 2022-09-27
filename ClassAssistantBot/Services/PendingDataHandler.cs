﻿using System.Text;
using ClassAssistantBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.BotAPI.AvailableTypes;
using User = ClassAssistantBot.Models.User;

namespace ClassAssistantBot.Services
{
    public class PendingDataHandler
    {
        private DataAccess dataAccess { get; set; }

        public PendingDataHandler(DataAccess dataAccess)
        {
            this.dataAccess = dataAccess;
        }

        public (string, int) GetPendings(User user, bool directPendings = false, InteractionType interactionType = InteractionType.None, int page = 1)
        {
            var pendings = new List<Pending>();
            if (!directPendings)
            {
                pendings = dataAccess.Pendings
                .Where(x => x.ClassRoomId == user.ClassRoomActiveId)
                .Include(x => x.Student)
                .ThenInclude(x => x.User)
                .ToList();
                var removePendings = new List<Pending>();
                foreach (var pending in pendings)
                {
                    var directPending = dataAccess.DirectPendings.Where(x => x.PendingId == pending.Id).FirstOrDefault();
                    if (directPending != null)
                    {
                        removePendings.Add(pending);
                    }
                }

                pendings.RemoveAll(x => removePendings.Contains(x));
            }
            else
            {
                var directPending = dataAccess.DirectPendings
                    .Where(x => x.UserId == user.Id && x.Pending.ClassRoomId == user.ClassRoomActiveId)
                    .Include(x => x.Pending)
                    .Include(x => x.Pending.Student)
                    .Include(x => x.Pending.Student.User)
                    .ToList();
                foreach (var item in directPending)
                {
                    pendings.Add(item.Pending);
                }
            }

            var classRoom = dataAccess.ClassRooms
                .Where(x => x.Id == user.ClassRoomActiveId)
                .First();
            
            if(interactionType != InteractionType.None && !directPendings)
            {
                 pendings.RemoveAll(x => x.Type != interactionType);
            }

            user.Status = UserStatus.Pending;
            dataAccess.Users.Update(user);
            dataAccess.SaveChanges();
            int count = pendings.Count/10;
            if (pendings.Count % 10 != 0)
                count++;
            pendings = pendings.Skip((page - 1)*10).Take(10).ToList();
            var res = new StringBuilder($"Pendientes de la clase {classRoom.Name}:\n");

            foreach (var item in pendings)
            {
                res.Append(item.Type.ToString());
                res.Append(": ");
                if (!string.IsNullOrEmpty(item.Student.User.Name))
                    res.Append(item.Student.User.Name);
                else
                    res.Append(item.Student.User.FirstName + " " + item.Student.User.LastName);
                res.Append("(" + item.Student.User.Username);
                res.Append(") -> /");
                res.Append(item.Code);
                res.Append("\n");
            }
            if (pendings.Count == 0)
                res.Append("No tiene revisiones pendientes en esta aula");
            return (res.ToString(), count);
        }

        public string GetPendingByCode(string code, out string pend)
        {
            var res = new StringBuilder();
            var pending = dataAccess.Pendings
                .Where(x => x.Code == code)
                .FirstOrDefault();
            pend = "";
            if (pending == null)
                return pend;
            if (pending.Type == InteractionType.ClassIntervention)
            {
                var classIntervention = dataAccess.ClassInterventions
                    .Include(x => x.User)
                    .Include(x => x.Class)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"ClassIntervention de {classIntervention.User.Username}\n");
                res.Append($"Clase: {classIntervention.Class.Title}\n");
                res.Append($"Intevención: {classIntervention.Text}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }
            else if (pending.Type == InteractionType.ClassTitle)
            {
                var classTitle = dataAccess.ClassTitles
                    .Include(x => x.Class)
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"ClassTitle de {classTitle.User.Username}\n");
                res.Append($"Clase: {classTitle.Class.Title}\n");
                res.Append($"Título: {classTitle.Title}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }
            else if (pending.Type == InteractionType.Daily)
            {
                var daily = dataAccess.Dailies
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"Daily de {daily.User.Username}\n");
                res.Append($"Actualización: {daily.Text}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }
            else if (pending.Type == InteractionType.Joke)
            {
                var joke = dataAccess.Jokes
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"Joke de {joke.User.Username}\n");
                res.Append($"Texto: {joke.Text}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }
            else if (pending.Type == InteractionType.Meme)
            {
                var meme = dataAccess.Memes
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"Meme de {meme.User.Username}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
                pend = meme.FileId;
            }
            else if (pending.Type == InteractionType.RectificationToTheTeacher)
            {
                var rectificationToTheTeacher = dataAccess.RectificationToTheTeachers
                    .Include(x => x.Teacher.User)
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"RectificationToTheTeachers de {rectificationToTheTeacher.User.Username}\n");
                res.Append($"Profesor: {rectificationToTheTeacher.Teacher.User.Username}\n");
                res.Append($"Texto: {rectificationToTheTeacher.Text}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }
            else
            {
                var statusphrase = dataAccess.StatusPhrases
                    .Include(x => x.User)
                    .First(x => x.Id == pending.ObjectId);
                res.Append($"StatusPhrases de {statusphrase.User.Username}\n");
                res.Append($"Frase: {statusphrase.Phrase}\n");
                res.Append($"Código de Pendiente: /{pending.Code}\n");
            }

            return res.ToString();
        }

        public Pending GetPending(string code)
        {
            return dataAccess.Pendings
                .Where(x => x.Code == code)
                .Include(x => x.Student)
                .Include(x => x.Student.User)
                .Include(x => x.ClassRoom)
                .First();
        }

        public void RemovePending(Pending pending)
        {
            var directPendings = dataAccess.DirectPendings.Where(x => x.PendingId == pending.Id).ToList();
            dataAccess.Pendings.Remove(pending);
            dataAccess.DirectPendings.RemoveRange(directPendings);
            dataAccess.SaveChanges();
        }

        public long AddDirectPending(string username, string pendingId)
        {
            var user = dataAccess.Users.Where(x => x.Username == username || x.Username == username.Substring(1)).First();

            var directPending = new DirectPending
            {
                Id = Guid.NewGuid().ToString(),
                PendingId = pendingId,
                UserId = user.Id
            };
            dataAccess.DirectPendings.Add(directPending);
            dataAccess.SaveChanges();
            return user.ChatId;
        }

        public string GetAllClassRoomWithPendings(User user)
        {
            var classRooms = dataAccess.TeachersByClassRooms
                            .Where(x => x.Teacher.UserId == user.Id)
                            .Include(x => x.ClassRoom)
                            .ToList();
            StringBuilder res = new StringBuilder();
            int i = 0;
            foreach (var classRoom in classRooms)
            {
                if (dataAccess.Pendings.Where(x=>x.ClassRoomId==classRoom.ClassRoomId).Count()!=0)
                {
                    res.Append(++i);
                    res.Append(": ");
                    res.Append(classRoom.ClassRoom.Name);
                    res.Append("\n");
                }
            }

            return res.ToString();
        }
    }
}

