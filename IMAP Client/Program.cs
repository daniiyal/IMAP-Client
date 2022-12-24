
using System.Security.Cryptography;
using IMAP_Client;

var client = new Client("127.0.0.1", 143);

await client.ConnectServerAsync();

await client.Login("myman@mymail.com", "123");
var boxList = await client.List("\"\" \"*\"");

foreach (var box in boxList)
{
    Console.WriteLine($"Папка {box}");
}

await client.Select("INBOX");
var uids = await client.SearchAll();
foreach (var uid in uids)
{
    Console.WriteLine($"UID - {uid}");
}
await client.Fetch("1", "ALL");
var mail = await client.Fetch("1", "BODY[]");
foreach(var k in mail)
{
    Console.WriteLine($"{k.Key}: {k.Value}");
}
await client.Close();
await client.Logout();


