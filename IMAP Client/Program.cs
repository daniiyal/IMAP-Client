
using System.Security.Cryptography;
using IMAP_Client;

var client = new Client("127.0.0.1", 143);

await client.ConnectServerAsync();

await client.Login("adasda", "1568452");



