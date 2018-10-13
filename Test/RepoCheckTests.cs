﻿using Install;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CompressImagesFunction;
using Common;
using Common.Messages;
using Common.TableModels;
using Microsoft.Azure.WebJobs;

namespace Test
{
    [TestClass]
    public class RepoCheckTests
    {
        [TestMethod]
        public async Task GivenArchivedRepo_ShouldSkip()
        {
            await ExecuteRunAsync(12345, "dabutvin", "test", 150, out var logger);

            logger.AssertCallCount(2);
            logger.SecondCall().AssertLogLevel(LogLevel.Information);
            logger.SecondCall().AssertLogMessage("CompressImagesFunction: skipping archived repo {Owner}/{RepoName}");
            logger.SecondCall().AssertLogValues(
                KeyValuePair.Create("Owner", "dabutvin"),
                KeyValuePair.Create("RepoName", "test"));
        }

        private Task ExecuteRunAsync(int installationId, string owner, string repoName, long prId, out ILogger logger)
        {
            var cloneUrl = $"https://github.com/{owner}/{repoName}";

            var compressImagesMessage = new CompressImagesMessage
            {
                CloneUrl = cloneUrl,
                Owner = owner,
                InstallationId = installationId,
                RepoName = repoName
            };

            return ExecuteRunAsync(compressImagesMessage, prId, out logger);
        }

        private Task ExecuteRunAsync(CompressImagesMessage compressImagesMessage, long prId, out ILogger logger)
        {
            logger = Substitute.For<ILogger>();

            var context = Substitute.For<ExecutionContext>();
            context.FunctionDirectory = "data/functiondir";

            var installationTokenProvider = Substitute.For<IInstallationTokenProvider>();
            installationTokenProvider
                 .GenerateAsync(Arg.Any<InstallationTokenParameters>(), Arg.Any<StreamReader>())
                 .Returns(Task.FromResult(new InstallationToken
                 {
                     Token = "token",
                     ExpiresAt = "12345"
                 }));

            var openPrMessages = Substitute.For<ICollector<OpenPrMessage>>();

            var repoChecks = Substitute.For<IRepoChecks>();
            repoChecks.IsArchived(Arg.Any<GitHubClientParameters>())
                .Returns(x => Task.FromResult(true));

            return CompressImagesFunction.CompressImagesFunction.RunAsync(
                installationTokenProvider,
                compressImagesMessage,
                openPrMessages,
                repoChecks,
                logger,
                context);
        }
    }
}
