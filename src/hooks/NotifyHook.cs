using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using Nancy;
using Newtonsoft.Json;
using RazorEngine;

namespace GithubTools.Hooks
{
	public class NotifyHook : NancyModule
	{
		public NotifyHook(IRootPathProvider rootPathProvider)
		{
			Post["/notify"] = x =>
			{
				var rootPath = rootPathProvider.GetRootPath();
				bool skipEmail;
				bool.TryParse(Request.Query["skipEmail"], out skipEmail);

				PushData pushData = JsonConvert.DeserializeObject<PushData>(Request.Form["payload"]);

				var oldestCommit = pushData.Commits.OrderBy(c => c.Timestamp).First();
				var oldestCommitMsgFirstLine = oldestCommit.Message.Split('\n')[0];

				var subject = string.Format("[{0}/{1}, {2}] ({3}) {4}",
					pushData.Repository.Owner.Name,
					pushData.Repository.Name,
					pushData.Branch,
					oldestCommit.Author.Username,
					oldestCommitMsgFirstLine);

				var template = File.ReadAllText(Path.Combine(rootPath, @"Views", @"Notify.cshtml"));
				var body = Razor.Parse(template, pushData);

				if (!skipEmail)
				{
					var mailMessage = new MailMessage
					{
						From = new MailAddress("github@avispl.com", "GitHub (AVI-SPL)"),
						Subject = subject,
						Body = body,
						IsBodyHtml = true
					};
					mailMessage.ReplyToList.Add("applicationsdevelopment@avispl.com");
					mailMessage.Headers.Add("gh-repo", pushData.Repository.Name);
					mailMessage.To.Add(ConfigurationManager.AppSettings["EmailsTo"]);
					new SmtpClient().Send(mailMessage);
				}
				return body;
			};
		}
	}

	public class PushData
	{
		private string _ref;
		public string Ref
		{
			get { return _ref; }
			set
			{
				_ref = value;
				Branch = _ref.Replace("refs/heads/", "");
			}
		}
		public string Branch { get; set; }
		public RepoData Repository { get; set; }
		public List<CommitData> Commits { get; set; }
	}

	public class RepoData
	{
		public string Name { get; set; }
		public string Url { get; set; }
		public UserData Owner { get; set; }
	}

	public class CommitData
	{
		public UserData Author { get; set; }
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
		public string Url { get; set; }
		public string Id { get; set; }
		public List<string> Modified { get; set; }
		public List<string> Added { get; set; }
		public List<string> Removed { get; set; }

		private List<FileData> _files;
		public List<FileData> Files
		{
			get
			{
				if (_files == null)
				{
					var files = Modified.Select(f => new FileData { Action = "Modified", File = f });
					files = files.Union(Added.Select(f => new FileData { Action = "Added", File = f }));
					files = files.Union(Removed.Select(f => new FileData { Action = "Deleted", File = f }));
					_files = files.OrderBy(fd => fd.File).ToList();
				}
				return _files;
			}
		}
	}

	public class FileData
	{
		public string Action { get; set; }
		public string File { get; set; }
	}

	public class UserData
	{
		public string Name { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
	}
}