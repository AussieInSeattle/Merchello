﻿using System;
using System.Xml;
using Merchello.Core.Configuration;
using Umbraco.Web;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.DataLayer;
using umbraco.interfaces;
using Umbraco.Core.Logging;

namespace Merchello.Web.PackageActions
{
	/// <summary>
	/// This package action will grant a user access to an Umbraco application.
	/// </summary>
	/// <remarks>
	/// This package action has been customized from the PackageActionsContrib Project.
	/// http://packageactioncontrib.codeplex.com
	/// </remarks>
	public class GrantPermissionForApp : IPackageAction
	{
		public string Alias()
		{
			return string.Concat(MerchelloConfiguration.ApplicationName, "_GrantPermissionForApp");
		}

		public bool Execute(string packageName, XmlNode xmlData)
		{
			// execute revoke first to clear any existing permissions app/user relationships
			Revoke(packageName, xmlData);
			return Grant(packageName, xmlData);
		}

		public bool Undo(string packageName, XmlNode xmlData)
		{
			return Revoke(packageName, xmlData);
		}

		public XmlNode SampleXml()
		{
			var sample = string.Concat("<Action runat=\"install\" undo=\"false\" alias=\"", Alias(), "\" userLogin=\"$currentUser\" appName=\"", MerchelloConfiguration.ApplicationName.ToLowerInvariant(), "\"/>");
			return helper.parseStringToXmlNode(sample);
		}

		private bool Grant(string packageName, XmlNode xmlData)
		{
			const string grantSql = "INSERT INTO umbracoUser2app ([user], app) SELECT id, @AppName FROM umbracoUser WHERE userLogin = @UserLogin";
			return ExecutePermissionSql(packageName, xmlData, grantSql);
		}

		private bool Revoke(string packageName, XmlNode xmlData)
		{
			const string revokeSql = "DELETE umbracoUser2app FROM umbracoUser2app JOIN umbracoUser ON umbracoUser2App.[user] = umbracoUser.id WHERE umbracoUser.userLogin = @UserLogin AND umbracoUser2App.app = @AppName";
			return ExecutePermissionSql(packageName, xmlData, revokeSql);
		}

		private bool ExecutePermissionSql(string packageName, XmlNode xmlData, string sql)
		{
			var appName = this.GetAttributeValue(xmlData, "appName");
			var userLogin = this.GetAttributeValue(xmlData, "userLogin");

		    if (string.Equals(userLogin, "$currentUser", StringComparison.OrdinalIgnoreCase))
		        userLogin = UmbracoContext.Current.UmbracoUser.LoginName; //UmbracoEnsuredPage.CurrentUser.LoginName;

			var userNameParam = Application.SqlHelper.CreateParameter("@UserLogin", userLogin);
			var appNameParam = Application.SqlHelper.CreateParameter("@AppName", appName);

			try
			{
				Application.SqlHelper.ExecuteNonQuery(sql, userNameParam, appNameParam);
				return true;
			}
			catch (SqlHelperException ex)
			{
				LogHelper.Error<GrantPermissionForApp>(string.Format("Error in Grant User Permission for App action for package {0}.", packageName), ex);
			}

			return false;
		}

		private string GetAttributeValue(XmlNode node, string attributeName)
		{
			if (node.Attributes[attributeName] != null)
			{
				var result = node.Attributes[attributeName].InnerText;
				if (!string.IsNullOrEmpty(result))
					return result;
			}

			return string.Empty;
		}
	}
}