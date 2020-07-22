using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ASP.NET_Project.Services
{
	public interface IAzureBlobService
	{
		
		Task<Uri> UploadAsync(IFormFileCollection files,string name);
		Task DeleteAsync(string fileUri);
	
       
    }

	public class AzureBlobService : IAzureBlobService
	{
		private readonly IAzureBlobConnectionFactory _azureBlobConnectionFactory;

		public AzureBlobService(IAzureBlobConnectionFactory azureBlobConnectionFactory)
		{
			_azureBlobConnectionFactory = azureBlobConnectionFactory;
		}

		

		public async Task DeleteAsync(string fileUri)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			Uri uri = new Uri(fileUri);
			string filename = Path.GetFileName(uri.LocalPath);

			var blob = blobContainer.GetBlockBlobReference(filename);
			await blob.DeleteIfExistsAsync();
		}

  

   
		public async Task<Uri> UploadAsync(IFormFileCollection files,string name)
		{
			var blobContainer = await _azureBlobConnectionFactory.GetBlobContainer();

			
				var blob = blobContainer.GetBlockBlobReference(name);
            using (var stream = files[0].OpenReadStream())
            {
                await blob.UploadFromStreamAsync(stream);

            }
            return blob.Uri;
			
		}

	}
}
