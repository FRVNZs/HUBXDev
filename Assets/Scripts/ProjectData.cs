using System;

[Serializable]
public class ProjectData
{
    public string id;

    public string title;
    public string version;
    public string description;
    public string createdAt;
    public string updatedAt;
    public string githubUrl;

    public string coverImageFile;
    public string detailImageFile;

    [NonSerialized] public string coverImagePath;
    [NonSerialized] public string detailImagePath;
}