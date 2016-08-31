using Google.Apis.Download;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using System;
using System.Collections.Generic;
using System.IO;
namespace GoogleStorageProvider
{
    public class GSProvider : IDisposable
    {

        /// <summary>
        /// 專案ID
        /// </summary>
        public string _projectId { private set; get; }

        /// <summary>
        /// Bucket名稱
        /// </summary>
        public string _bucketName { set; get; }

        private StorageService _storage;

        /// <summary>
        /// Google Storage Provider
        /// </summary>
        /// <param name="credentialsPath">JSON憑證位置</param>
        /// <param name="projectId">專案ID</param>
        /// <param name="bucketName">Bucket名稱</param>
        public GSProvider(string credentialsPath, string projectId, string bucketName)
        {
            this._projectId = projectId;
            this._bucketName = bucketName;

            _storage = CreateStorageClient(credentialsPath);
        }

        /// <summary>
        /// Google Storage Provider
        /// </summary>
        /// <param name="credentialsPath">JSON憑證Stream</param>
        /// <param name="projectId">專案ID</param>
        /// <param name="bucketName">Bucket名稱</param>
        public GSProvider(Stream credential, string projectId, string bucketName)
        {
            this._projectId = projectId;
            this._bucketName = bucketName;

            _storage = CreateStorageClient(credential);
        }

        private StorageService CreateStorageClient(Stream jsonStream)
        {
            var credentials = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(jsonStream);
            if (credentials.IsCreateScopedRequired)
            {
                credentials = credentials.CreateScoped(new[] { StorageService.Scope.DevstorageFullControl });
            }

            var serviceInitializer = new BaseClientService.Initializer()
            {
                ApplicationName = "FileService",
                HttpClientInitializer = credentials,
            };

            return new StorageService(serviceInitializer);
        }

        /// <summary>
        /// Create Client
        /// </summary>
        /// <returns></returns>
        private StorageService CreateStorageClient(string credentialsPath)
        {
            using (Stream jsonStream = new FileStream(credentialsPath, FileMode.Open))
            {
                return CreateStorageClient(jsonStream);
            }
        }

        /// <summary>
        /// 列出所有Bucket
        /// </summary>
        /// <returns></returns>
        public IList<Bucket> ListBuckets()
        {
            var buckets = _storage.Buckets.List(_projectId).Execute();

            return buckets.Items;
        }

        /// <summary>
        /// 列出Bucket中的檔案
        /// </summary>
        /// <param name="bucketName"></param>
        public IList<Google.Apis.Storage.v1.Data.Object> ListObjects()
        {
            var objects = _storage.Objects.List(_bucketName).Execute();

            return objects.Items;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="fileStream">檔案來源</param>
        /// <param name="PathWithFileName">目標路徑與檔案名稱</param>
        /// <param name="contentType">檔案型別</param>
        /// <returns></returns>
        public bool UploadStream(Stream fileStream, string PathWithFileName, string contentType)
        {
            PathWithFileName = PathWithFileName.Replace("\\", "/");

            var result = _storage.Objects.Insert(
                            bucket: _bucketName,
                            stream: fileStream,
                            contentType: contentType,
                            body: new Google.Apis.Storage.v1.Data.Object() { Name = PathWithFileName }
                        ).Upload();

            return result.Status == Google.Apis.Upload.UploadStatus.Completed;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="fileStream">檔案來源</param>
        /// <param name="PathWithFileName">目標路徑與檔案名稱</param>
        /// <returns></returns>
        public bool UploadStream(Stream fileStream, string PathWithFileName)
        {
            return UploadStream(fileStream, PathWithFileName, MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(PathWithFileName)));
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="fileStream">檔案來源</param>
        /// <param name="PathWithFileName">目標路徑與檔案名稱</param>
        /// <returns></returns>
        public bool UploadStream(byte[] fileBytes, string PathWithFileName)
        {
            using (var fileStream = new MemoryStream(fileBytes))
            {
                return UploadStream(fileStream, PathWithFileName, MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(PathWithFileName)));
            }
        }

        /// <summary>
        /// Get File 
        /// </summary>
        /// <param name="PathWithFileName">目標路徑與檔案名稱</param>
        /// <returns></returns>
        public byte[] DownloadStream(string PathWithFileName)
        {
            PathWithFileName = PathWithFileName.Replace("\\", "/");
            using (var stream = new MemoryStream())
            {
                _storage.Objects.Get(_bucketName, PathWithFileName).Download(stream);

                var bytes = stream.ToArray();

                return bytes;
            }
        }

        /// <summary>
        /// 刪除檔案
        /// </summary>
        /// <param name="PathWithFileName">目標路徑與檔案名稱</param>
        public bool DeleteObject(string PathWithFileName)
        {
            PathWithFileName = PathWithFileName.Replace("\\", "/");
            try
            {
                var result = _storage.Objects.Delete(_bucketName, PathWithFileName).Execute();
                return true;
            }
            catch (Google.GoogleApiException)
            {
                return false;
            }
        }

        /// <summary>
        /// 取回檔案儲存
        /// </summary>
        /// <param name="sourcePathWithFileName">來源路徑與檔案名稱</param>
        /// <param name="targetPathWithFileName">目標路徑與檔案名稱</param>
        /// <returns></returns>
        public bool DownloadToFile(string sourcePathWithFileName, string targetPathWithFileName)
        {
            sourcePathWithFileName = sourcePathWithFileName.Replace("\\", "/");

            var objectToDownload = _storage.Objects.Get(_bucketName, sourcePathWithFileName).Execute();

            var downloader = new MediaDownloader(_storage);

            downloader.ProgressChanged += progress =>
            {
                Console.WriteLine($"{progress.Status} {progress.BytesDownloaded} bytes");
            };

            using (var fileStream = new FileStream(targetPathWithFileName, FileMode.Create))
            {
                var progress = downloader.Download(objectToDownload.MediaLink, fileStream);

                if (progress.Status == DownloadStatus.Completed)
                {
                    Console.WriteLine($"Downloaded {sourcePathWithFileName} to {targetPathWithFileName}");
                    return true;
                }
            }

            return false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 偵測多餘的呼叫

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置 Managed 狀態 (Managed 物件)。
                    _storage.Dispose();
                    _storage = null;
                }

                // TODO: 釋放 Unmanaged 資源 (Unmanaged 物件) 並覆寫下方的完成項。
                // TODO: 將大型欄位設為 null。

                disposedValue = true;
            }
        }

        // TODO: 僅當上方的 Dispose(bool disposing) 具有會釋放 Unmanaged 資源的程式碼時，才覆寫完成項。
        // ~GoogleStorageProvider() {
        //   // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 加入這個程式碼的目的在正確實作可處置的模式。
        public void Dispose()
        {
            // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果上方的完成項已被覆寫，即取消下行的註解狀態。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

}


