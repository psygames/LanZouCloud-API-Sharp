using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using LanZouCloudAPI;
using System;
using System.Threading;

namespace Test
{
    [TestClass]
    public class DownloadTest
    {
        [TestMethod]
        public async Task DownloadFileById()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;

            var fileId = await AConfig.GetTestFile(cloud);

            var info = await cloud.DownloadFile(fileId, "./", null, true,
                new Progress<ProgressInfo>(_progress =>
                {
                    if (_progress.state == ProgressState.Start)
                        isStartOK = true;
                    if (_progress.state == ProgressState.Ready)
                        isReadyOK = true;
                    if (_progress.state == ProgressState.Progressing)
                        isDownloadingOK = true;
                    if (_progress.state == ProgressState.Finish)
                        isFinishOK = true;
                }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(File.Exists(info.filePath));

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isDownloadingOK);
            Assert.IsTrue(isFinishOK);
        }

        [TestMethod]
        public async Task DownloadFileBigById()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            var fileId = await AConfig.GetTestFileBig(cloud);

            var info = await cloud.DownloadFile(fileId, "./", null, true);

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
        }

        [TestMethod]
        public async Task DownloadFileBigAndCancelById()
        {
            var cloud = await AConfig.AsyncLoginCloud();
            var fileList = await cloud.GetFileList();
            Assert.IsTrue(fileList.code == LanZouCode.SUCCESS);

            var fileId = await AConfig.GetTestFileBig(cloud);

            var cts = new CancellationTokenSource();

            new Task(async () =>
            {
                await Task.Delay(4000);
                cts.Cancel();
            }).Start();

            var info = await cloud.DownloadFile(fileId, "./", null, true, null, cts.Token);

            Assert.IsTrue(info.code == LanZouCode.TASK_CANCELED);
        }

        [TestMethod]
        public async Task DownloadFileByUrl()
        {
            var cloud = AConfig.Cloud();

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;
            var url = "https://psyduck.lanzoui.com/ibf1Xvlo4rc";
            // var url = "https://psyduck.lanzoui.com/idyVcwcu06f";
            var info = await cloud.DownloadFileByUrl(url, "./", null, "95w3", true,
                new Progress<ProgressInfo>(_progress =>
                {
                    if (_progress.state == ProgressState.Start)
                        isStartOK = true;
                    if (_progress.state == ProgressState.Ready)
                        isReadyOK = true;
                    if (_progress.state == ProgressState.Progressing)
                        isDownloadingOK = true;
                    if (_progress.state == ProgressState.Finish)
                        isFinishOK = true;
                }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(File.Exists(info.filePath));

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isDownloadingOK);
            Assert.IsTrue(isFinishOK);
        }

        [TestMethod]
        public async Task DownloadFileBigByUrl()
        {
            var cloud = AConfig.Cloud();

            bool isStartOK = false;
            bool isReadyOK = false;
            bool isDownloadingOK = false;
            bool isFinishOK = false;
            var url = "https://psyduck.lanzoui.com/iDCSnvmho6f";
            var info = await cloud.DownloadFileByUrl(url, "./", null, null, true,
                new Progress<ProgressInfo>(_progress =>
                {
                    if (_progress.state == ProgressState.Start)
                        isStartOK = true;
                    if (_progress.state == ProgressState.Ready)
                        isReadyOK = true;
                    if (_progress.state == ProgressState.Progressing)
                        isDownloadingOK = true;
                    if (_progress.state == ProgressState.Finish)
                        isFinishOK = true;
                }));

            Assert.IsTrue(info.code == LanZouCode.SUCCESS);
            Assert.IsTrue(!string.IsNullOrEmpty(info.url));
            Assert.IsTrue(!string.IsNullOrEmpty(info.fileName));
            Assert.IsTrue(File.Exists(info.filePath));

            Assert.IsTrue(isStartOK);
            Assert.IsTrue(isReadyOK);
            Assert.IsTrue(isDownloadingOK);
            Assert.IsTrue(isFinishOK);
        }


    }
}
