using SQLite;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace MauiApp2
{
    internal class User
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string? Username { get; set; }

        // Store password hash
        public string? PasswordHash { get; set; }
    }
}
