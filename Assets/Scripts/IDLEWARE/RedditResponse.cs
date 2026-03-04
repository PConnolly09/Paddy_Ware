using System;

[Serializable]
public class RedditResponse
{
    public RedditData data;
}

[Serializable]
public class RedditData
{
    public RedditPost[] children;
}

[Serializable]
public class RedditPost
{
    public RedditPostData data;
}

// UPGRADED: Now pulls the deep analytics required for the Market Engine
[Serializable]
public class RedditPostData
{
    public string title;
    public string selftext;
    public int score;
    public int num_comments;
    public float upvote_ratio;
    public string subreddit;
    public int subreddit_subscribers;
    public float created_utc;
}