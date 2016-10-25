# PRCat
This is a custom incoming webhook integration of slack

It can pull Github PRs from your team, regroup them and post them on slack channels

To make it work, you should have

* A GitHub token with read access to your repos and team member infos
* A Slack incoming webhook url
* A Microsoft Azure blob connection string
* Correct configuration on the appSettings section

To config it, see the appsettings example below

```xml
 <appSettings>
 
    <!--App related configs-->
    <!--Will ignore PRs if the names are not in this list-->
    <add key="GitHubMembers" value="bgross,dhoizner,izlobinskiy,nshahid-mdsol,mkhuspe,tychen-mdsol,sjakir,vorman,Vascods,zcao,mmatta,zichen-mdsol,vcapers-mdsol,smicalizzi-mdsol,rmosquera,pmartinez-mdsol,mglintz-mdsol,mnohai-mdsol,Mansi-Shah,georgeblake,dcassidy-mdsol,gtaylor-mdsol,briansingh-mdsol,amonticchio,vikaschoithani,davidjetter,vraj,echen-mdsol,chenghuang-mdsol,fbotero-mdsol,hrivera,art2003,chwilliams,sjakir,vyaroshevskiy,junshao,mrobinson-mdsol,mzibkoff,klofton" />
    <!--PRs with these tags are treated as not important-->
    <add key="LessImportantTags" value="NRTM, postponed, donotmerge,Housekeeping" />
    <!--Epoch ticks, PRs which are not updated within this time span are considered to be overdue-->
    <add key="OverdueTicks" value="172800" />
    <!--Monitored Repos-->
    <add key="GitHubRepos" value="mdsol/Gambit,mdsol/Rave,mdsol/ravegarage" />
    <!--Git hub token-->
    <add key="GitHubToken" value="{your GitHub token}" />
    <!--Configure it in slack first-->
    <add key="SendToSlackUrl" value="{your slack incoming webhook url}" />
    <!--Azure blob connection string-->
    <add key="AzureBlob.ConnectionString" value="{your azure blob connection string}"/>
    <!--Azure blob container name, will create if not exist-->
    <add key="AzureBlob.Container" value="daily-pr-reports"/>
  </appSettings>
```

Screenshots:



