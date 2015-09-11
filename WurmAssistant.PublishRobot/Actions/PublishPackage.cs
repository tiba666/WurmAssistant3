﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AldursLab.Essentials.Configs;
using AldursLab.Essentials.Extensions.DotNet.Collections.Generic;
using AldursLab.Essentials.FileSystem;
using AldursLab.WurmAssistant.PublishRobot.Parts;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;

namespace AldursLab.WurmAssistant.PublishRobot.Actions
{
    class PublishPackage
    {
        readonly string tempDir;

        readonly string buildCode;
        readonly string buildNumber;
        readonly string releasesInfoPath;
        readonly string packageBinPath;
        readonly string webServiceRootUrl;
        readonly string webServiceControllerPath;
        readonly string webServiceLogin;
        readonly string webServicePassword;
        readonly string slackIntegrationSubUrl;

        readonly string latestMinorVersionString;

        readonly IOutput output;

        public PublishPackage(IConfig config, [NotNull] string tempDir, [NotNull] IOutput output)
        {
            if (tempDir == null) throw new ArgumentNullException("tempDir");
            if (output == null) throw new ArgumentNullException("output");
            this.tempDir = tempDir;
            this.output = output;

            buildCode = config.GetValue("build code");
            buildNumber = config.GetValue("build number");
            releasesInfoPath = config.GetValue("releases info path");
            packageBinPath = config.GetValue("package bin path");
            webServiceRootUrl = config.GetValue("web service root url");
            webServiceControllerPath = config.GetValue("web service controller path");
            webServiceLogin = config.GetValue("web service login");
            webServicePassword = config.GetValue("web service password");
            slackIntegrationSubUrl = config.GetValue("slack integration sub url");

            latestMinorVersionString = GetLatestMinorVersionString();
        }

        public void Execute()
        {
            var publisher = new PublishingWebService(output,
                webServiceRootUrl,
                webServiceControllerPath,
                webServiceLogin,
                webServicePassword);
            var slacker = new SlackService(output, slackIntegrationSubUrl);

            var binDir = new DirectoryInfo(packageBinPath);

            DirectoryOps.CopyRecursively(binDir.FullName, binDir.FullName);

            var targetVersionDatFile = new FileInfo(Path.Combine(binDir.FullName, "version.dat"));
            File.WriteAllText(targetVersionDatFile.FullName, BuildVersionDatContents());

            var zipper = new FastZip();
            var zipFile = new FileInfo(Path.Combine(tempDir, string.Format("{0}.zip", BuildFileName())));
            zipper.CreateZip(zipFile.FullName, binDir.FullName, true, null);

            publisher.Publish(zipFile, buildCode, buildNumber);
            output.Write("Publishing operation completed.");

            slacker.SendMessage(string.Format("Published {0}", BuildFileName()));
        }

        string BuildFileName()
        {
            return string.Format("WurmAssistant-{0}-{1}-R{2}", latestMinorVersionString, buildCode, buildNumber);
        }

        string BuildVersionDatContents()
        {
            return string.Format("{0}\n{1}\n{2}\n{3}",
                latestMinorVersionString,
                buildCode,
                buildNumber,
                DateTimeOffset.Now.ToString("O"));
        }

        string GetLatestMinorVersionString()
        {
            var releaseDirs = GetReleaseDirInfos();
            var version = releaseDirs.Max(info => info.Version);
            return string.Format("{0}.{1}", version.Major, version.Minor);
        }

        ReleaseDirInfo[] GetReleaseDirInfos()
        {
            var releaseDirInfo = new DirectoryInfo(releasesInfoPath);
            var releaseDirs = releaseDirInfo.GetDirectories().Select(info =>
            {
                Version v = null;
                Version.TryParse(info.Name, out v);
                return new ReleaseDirInfo(info, v);
            }).Where(r => r.Version != null).ToArray();

            if (releaseDirs.None())
            {
                throw new InvalidOperationException(string.Format("No release subdirectories found at location {0}",
                    releaseDirInfo.FullName));
            }
            return releaseDirs;
        }
    }

    class ReleaseDirInfo
    {
        public ReleaseDirInfo(DirectoryInfo directoryInfo, Version version)
        {
            DirectoryInfo = directoryInfo;
            Version = version;
        }

        public DirectoryInfo DirectoryInfo { get; private set; }
        public Version Version { get; private set; }
    }
}
