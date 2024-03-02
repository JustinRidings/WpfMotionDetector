using System;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;

namespace WebcamMotionDetector
{
#nullable enable
    internal class BlobStorageHelper
    {
        static internal BlobServiceClient? ServiceClient { get; set; }

        internal BlobStorageHelper(string sasUri)
        {
            Uri blobUri = new Uri(sasUri);
            ServiceClient = new BlobServiceClient(blobUri);
        }

        /// <summary>
        /// Uploads the content of a directory to cloud storage and then deletes it afterwards.
        /// </summary>
        /// <param name="localDirectory">Directory to upload / delete</param>
        /// <returns></returns>
        internal async Task UploadDeleteDirAsync(string localDirectory)
        {
            string containerName = ""; // Your Blob Container name here, or use a config file

            BlobContainerClient? containerClient = ServiceClient?.GetBlobContainerClient(containerName);
            if (containerClient != null && !string.IsNullOrEmpty(localDirectory))
            {
                DirectoryInfo di = new DirectoryInfo(localDirectory);
                if (di.Exists)
                {
                    foreach (var fileName in Directory.EnumerateFiles(localDirectory))
                    {
                        var blobClient = containerClient.GetBlobClient($"{Path.GetFileName(fileName)}");
                        if (blobClient != null)
                        {
                            File.Copy(fileName, $"{Environment.CurrentDirectory}\\{Path.GetFileName(fileName)}");
                            try
                            {
                                await blobClient.UploadAsync($"{Path.GetFileName(fileName)}");
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    foreach (var file in Directory.EnumerateFiles(localDirectory))
                    {
                        try
                        {
                            File.Delete(file);
                            File.Delete($"{Environment.CurrentDirectory}\\{Path.GetFileName(file)}");
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
#nullable disable