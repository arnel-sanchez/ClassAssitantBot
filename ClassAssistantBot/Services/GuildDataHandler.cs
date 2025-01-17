﻿using System;
using System.Text;
using ClassAssistantBot.Models;
using Microsoft.EntityFrameworkCore;

namespace ClassAssistantBot.Services
{
    public class GuildDataHandler
    {
        private DataAccess dataAccess { get; set; }

        public GuildDataHandler(DataAccess dataAccess)
        {
            this.dataAccess = dataAccess;
        }

        public void CreateGuild(User user)
        {
            user.Status = UserStatus.CreateGuild;
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();
        }

        public void CreateGuild(User user, string name)
        {
            user.Status = UserStatus.Ready;

            var guild = new Guild
            {
                Name = name,
                ClassRoomId = user.ClassRoomActiveId
            };

            dataAccess.Guilds.Add(guild);
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();
        }

        public List<Guild> DeleteGuild(User user)
        {
            user.Status = UserStatus.DeleteGuild;
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return dataAccess.Guilds.Where(x => x.ClassRoomId == user.ClassRoomActiveId).ToList();
        }

        public (string,List<Student>) DeleteGuild(User user, long id)
        {
            user.Status = UserStatus.Ready;

            var guild =  dataAccess.Guilds
                .Include(x => x.Students)
                .ThenInclude(x => x.User)
                .First(x => x.Id == id);

            dataAccess.Guilds.Remove(guild);
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return (guild.Name, guild.Students);
        }

        public List<Guild> AssignStudentAtGuild(User user)
        {
            user.Status = UserStatus.AssignStudentAtGuild;
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return dataAccess.Guilds.Where(x => x.ClassRoomId == user.ClassRoomActiveId).ToList();
        }

        public List<Student> AssignStudentAtGuild(User user, long guildId)
        {
            var studentsOnGuild =  dataAccess.Guilds
                .Where(x => x.Id == guildId)
                .Include(x => x.Students)
                .First();
            var studentsOnClassRoom =  dataAccess.StudentsByClassRooms
                .Include(x => x.Student)
                .Include(x => x.Student.User)
                .Where(x => x.ClassRoomId == user.ClassRoomActiveId)
                .ToList();

            var res = new List<Student>();

            foreach (var item in studentsOnClassRoom)
            {
                if (!studentsOnGuild.Students.Contains(item.Student))
                {
                    res.Add(item.Student);
                }
            }

            return res;
        }

        public (string, long) AssignStudentAtGuild(User user, long guildId, string studentId)
        {
            user.Status = UserStatus.Ready;

            var guild =  dataAccess.Guilds.Where(x => x.Id == guildId).First();
            var student =  dataAccess.Students
                .Include(x => x.User)
                .Where(x => x.Id == studentId)
                .First();

            guild.Students.Add(student);
            dataAccess.Users.Update(user);

             dataAccess.SaveChanges();
            return (guild.Name, student.User.ChatId);
        }

        public List<Guild> AssignCreditsAtGuild(User user)
        {
            user.Status = UserStatus.AssignCreditsAtGuild;
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return  dataAccess.Guilds.Where(x => x.ClassRoomId == user.ClassRoomActiveId).ToList();
        }

        public (List<Student>, long, string) AssignCreditsAtGuild(User user, string text)
        {
            user.Status = UserStatus.Ready;

            var data = text.Split(" ");

            var guildId = long.Parse(data[0]);
            var creditsCount = long.Parse(data[1]);
            string explication = data[2];

            for (int i = 3; i < data.Length; i++)
            {
                explication += " ";
                explication += data[i];
            }

            var guild =  dataAccess.Guilds
                .Include(x => x.Students)
                .ThenInclude(x => x.User)
                .Where(x => x.Id == guildId)
                .First();

            foreach (var student in guild.Students)
            {
                var random = new Random();
                var misc = new Miscellaneous
                {
                    ClassRoomId = user.ClassRoomActiveId,
                    DateTime = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    UserId = student.UserId,
                    Text = ""
                };
                var credit = new Credits
                {
                    ClassRoomId = user.ClassRoomActiveId,
                    DateTime = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    TeacherId = user.Id,
                    UserId = student.UserId,
                    Value = creditsCount,
                    Text = explication,
                    ObjectId = misc.Id,
                    Code = random.Next(10000, 99999)
                };
                dataAccess.Miscellaneous.Add(misc);
                dataAccess.Credits.Add(credit);
            }

            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return (guild.Students, creditsCount, explication);
        }

        public List<Guild> DeleteStudentFromGuild(User user)
        {
            user.Status = UserStatus.DeleteStudentFromGuild;
            dataAccess.Users.Update(user);
             dataAccess.SaveChanges();

            return dataAccess.Guilds.Where(x => x.ClassRoomId == user.ClassRoomActiveId).ToList();
        }

        public List<Student> DeleteStudentFromGuild(User user, long guildId)
        {
            var studentsOnGuild =  dataAccess.Guilds
                .Where(x => x.Id == guildId)
                .Include(x => x.Students)
                .ThenInclude(x => x.User)
                .First();

            return studentsOnGuild.Students;
        }

        public (string, long) DeleteStudentFromGuild(User user, long guildId, string studentId)
        {
            user.Status = UserStatus.Ready;

            var guild =  dataAccess.Guilds.Where(x => x.Id == guildId).First();
            var student =  dataAccess.Students
                .Include(x => x.User)
                .Where(x => x.Id == studentId)
                .First();

            guild.Students.Remove(student);
            dataAccess.Users.Update(user);

             dataAccess.SaveChanges();
            return (guild.Name, student.User.ChatId);
        }

        public List<Guild> DetailsGuild(User user)
        {
            user.Status = UserStatus.DetailsGuild;
            var guilds =  dataAccess.Guilds
                .Where(x => x.ClassRoomId == user.ClassRoomActiveId).ToList();

            dataAccess.Users.Update(user);

             dataAccess.SaveChanges();

            return guilds;
        }

        public string DetailsGuild(User user, long guildId)
        {
            user.Status = UserStatus.Ready;
            var guild =  dataAccess.Guilds
                .Include(x => x.Students)
                .ThenInclude(x => x.User)
                .Where(x => x.Id == guildId).First();

            var res = new StringBuilder($"Nombre: {guild.Name}\n\nEstudiantes:\n");

            int k = 1;
            foreach (var student in guild.Students)
            {
                res.Append($"{k}: {(string.IsNullOrEmpty(student.User.FirstName) ? student.User.Name : student.User.FirstName + " " + student.User.LastName)}\n");
                k++;
            }

            dataAccess.Users.Update(user);

             dataAccess.SaveChanges();

            return res.ToString();
        }
    }
}

