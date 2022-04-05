using MathematicGameApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Db
{
    public class MathematicGameDbContext : DbContext
    {
        public MathematicGameDbContext(DbContextOptions<MathematicGameDbContext> dbContext) : base(dbContext)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserFriend> UserFriends { get; set; }
        public DbSet<UserQuestionHistory> UserQuestionHistories { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<UserRoom> UserRooms { get; set; }
        public DbSet<InviteFriend> InviteFriends { get; set; }
        public DbSet<CheckEmail> CheckEmails { get; set; }

    }
}
