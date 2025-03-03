﻿namespace ApiCSharp.Models
{
    public class UserModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public DateOnly Birthday { get; set; }
        public string? FullName { get; set; }
    }
}
