Google Storage Provider
==============

Using Json Credential Create Storage Client.


Usage 

```csharp

var gsProvider = new GSProvider("[your Credential Path]","[projectID]","[bucketName]");

// Upload File
var fileName = "test.txt";
var content = "My text object content";
var uploadStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content));
bool result = gsProvider.UploadStream(uploadStream,fileName);
Console.WriteLine($"Upload {fileName} {result}");

// Download File
var fileName = "test.txt";
var data = gsProvider.DownloadStream(fileName);
var str = Encoding.UTF8.GetString(data);
Console.WriteLine($"Download {fileName} Success! ,Content={str}");
   
// Delete File
var fileName = "test.txt";
var result = gsProvider.DeleteObject(fileName);
Console.WriteLine($"Deleted {fileName} {result}!");
