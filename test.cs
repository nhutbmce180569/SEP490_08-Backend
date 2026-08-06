using System;

class Program {
    static void Main() {
        var duration = TimeSpan.FromMinutes(15);
        var expireTime = DateTime.UtcNow.Add(duration);
        string stored = expireTime.ToString("o");
        Console.WriteLine("Stored: " + stored);
        
        if (DateTime.TryParse(stored, out var parsed)) {
            Console.WriteLine("Parsed: " + parsed.ToString("o") + " Kind: " + parsed.Kind);
            var remaining = parsed - DateTime.UtcNow;
            Console.WriteLine("Remaining minutes: " + remaining.TotalMinutes);
            Console.WriteLine("Ceiled: " + Math.Ceiling(remaining.TotalMinutes));
        }
    }
}
