﻿namespace Lovelace.UserManagerAPI.Extensions
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; }
    }
}