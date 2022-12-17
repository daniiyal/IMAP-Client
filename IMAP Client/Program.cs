
using System.Security.Cryptography;
using IMAP_Client;

var client = new Client("127.0.0.1", 143);

await client.ConnectServerAsync();

await client.Login("qqqqq", "1568452");
await client.List("INBOX");
await client.Select("INBOX");
await client.Logout();


