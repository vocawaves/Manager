// See https://aka.ms/new-console-template for more information

using Manager.Services.BassAudio;

Console.WriteLine("Hello, World!");

var b = new BassAudioBackendService("Bass", 0);
await b.InitializeAsync();