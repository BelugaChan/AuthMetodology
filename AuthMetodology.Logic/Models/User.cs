using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Logic.Models
{
    public class User
    {
        private User(Guid id, string passwordHash, string email)
        {
            Id = id;
            PasswordHash = passwordHash;
            Email = email;
        }

        public Guid Id { get; set; }

        public string PasswordHash { get; private set; }

        public string Email { get; private set; }

        public static User Create(Guid id, string passwordHash, string email) =>
            new User(id, passwordHash, email);
    }
}
