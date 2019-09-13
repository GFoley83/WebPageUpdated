using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;

namespace WebPageUpdated
{
    public class BlobStorageService
    {
        private readonly string _storageCs;
        private static readonly string _containerName = "webpage-updated-screenshots";

        public BlobStorageService(string storageCs)
        {
            _storageCs = storageCs;
        }

        public async Task<string> UploadFileToBlob(string pathToFile)
        {
            if (!CloudStorageAccount.TryParse(_storageCs, out var storageAccount))
            {
                throw new ArgumentException("Wrong Azure Blob CS");
            }

            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);

            await cloudBlobContainer.CreateIfNotExistsAsync();

            //Upload file
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(pathToFile.Substring(pathToFile.LastIndexOf("\\") + 1));

            cloudBlockBlob.Properties.ContentType = "image/jpeg";

            await cloudBlockBlob.UploadFromFileAsync(pathToFile);

            return cloudBlockBlob.Uri.ToString();
        }

        public async Task DownloadFile(string path, string fileName)
        {
            if (!CloudStorageAccount.TryParse(_storageCs, out var storageAccount))
                throw new ArgumentException("Wrong Azure Blob CS");

            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);

            //Create blob if needed
            if (!await cloudBlobContainer.ExistsAsync())
                await cloudBlobContainer.CreateAsync();

            //Upload file
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            var target = Path.Combine(path, fileName);
            await cloudBlockBlob.DownloadToFileAsync(target, FileMode.Create);
        }
    }
}
