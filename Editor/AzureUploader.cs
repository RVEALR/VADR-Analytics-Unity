using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System;
using System.Text;
using System.Threading;

public delegate void FileUploadSuccessCallback(string filepath);
public delegate void FileUploadProgressCallback(int bytesUploaded);
public delegate void FileUploadErrorCallback(string filepath,string msg);
public class AzureUploader {
    string uploadUrl;
    byte[] bytes;
    int maxBlockSize = 256 * 1024;
    int totalBytesRemaining = 0;
    int currentFilePointer = 0;
    string filepath = "";
    List<string> blockIds;
    int bytesUploaded = 0;
    FileUploadProgressCallback progress;
    FileUploadSuccessCallback success;
    FileUploadErrorCallback error;
    private object _lock;
    public AzureUploader(string uploadUrl, string filepath, FileUploadSuccessCallback success,
        FileUploadProgressCallback progress, FileUploadErrorCallback error)
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        this.uploadUrl = uploadUrl;
        this.currentFilePointer = 0;
        this.filepath = filepath;
        this.blockIds = new List<string>();
        this.bytesUploaded = 0;
        this.success = success;
        this.error = error;
        this.progress = progress;
        this._lock = new object();
        try
        {
            this.bytes = System.IO.File.ReadAllBytes(filepath);
            this.totalBytesRemaining = this.bytes.Length;
        }
        catch(Exception e)
        {
            this.error(filepath, e.Message);
        }
    }

    public void startUpload()
    {
        while (this.totalBytesRemaining > 0)
        {
            if (this.totalBytesRemaining < this.maxBlockSize)
            {
                this.maxBlockSize = this.totalBytesRemaining;
            }
            byte[] outputBytes = new byte[this.maxBlockSize];
            Array.Copy(this.bytes, this.currentFilePointer, outputBytes, 0, this.maxBlockSize);
            int blockNumber = this.blockIds.Count;
            string blockId = string.Format("BlockId{0}", blockNumber.ToString("0000000"));
            string base64BlockId = Base64Encode(blockId);
            this.blockIds.Add(base64BlockId);
            this.currentFilePointer += this.maxBlockSize;
            this.totalBytesRemaining -= this.maxBlockSize;
            string url = this.uploadUrl + "&comp=block&blockId=" + this.blockIds[this.blockIds.Count - 1];
            this.uploadBlock(url, outputBytes);
        }
    }


    private string Base64Encode(string plainText)
    {
        return System.Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(plainText));
    }

    private void uploadBlock(string blockUrl, byte[] requestData)
    {
        Thread oThread = new Thread(() =>
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(blockUrl);
                request.Method = "PUT";
                request.Headers.Add("x-ms-blob-type", "BlockBlob");
                request.Headers.Add("x-ms-blob-cache-control", "public, max-age=864000");
                int contentLength = requestData.Length;
                request.ContentLength = contentLength;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(requestData, 0, contentLength);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if ((int)response.StatusCode == 201)
                    {
                        this.progress(requestData.Length);
                        lock (this._lock)
                        {
                            this.bytesUploaded += requestData.Length;
                            if (this.bytesUploaded == this.bytes.Length)
                            {
                                this.commitBlocks();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.error(this.filepath, e.Message);
            }
        });
        oThread.Start();
    }


    private void commitBlocks()
    {
        string url = this.uploadUrl + "&comp=blocklist";
        string requestData = "<?xml version=\"1.0\" encoding=\"utf-8\"?><BlockList>";
        for (int i = 0; i < this.blockIds.Count; i++)
        {
            requestData += "<Latest>" + this.blockIds[i] + "</Latest>";
        }
        requestData += "</BlockList>";
        Thread oThread = new Thread(() =>
        {
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "PUT";
                request.Headers.Add("x-ms-blob-content-type", "application/octet-stream");
                request.Headers.Add("x-ms-blob-cache-control", "public, max-age=864000");
                byte[] toBytes = Encoding.ASCII.GetBytes(requestData);
                request.ContentLength = toBytes.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(toBytes, 0, toBytes.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if ((int)response.StatusCode == 201)
                    {
                        this.success(this.filepath);
                    }
                }
            }
            catch (Exception e)
            {
                this.error(this.filepath, e.Message);
            }
        });
        oThread.Start();
    }
}
