package com.isaac.homedetector;

import android.content.Context;
import android.content.SharedPreferences;
import android.preference.PreferenceManager;
import android.util.Log;

import org.xmlpull.v1.XmlPullParserException;

import java.io.IOException;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.Calendar;
import java.util.List;

public class Utils 
{
    public static boolean isStringNullOrEmpty(String string)
    {
        if (string ==null) return true;
        if ( string != null && string. length () == 0 ){ return true ; }
        else
            return false;
    }
    public static Calendar getTimeAfterInSecs(int secs)
    {
        Calendar cal = Calendar.getInstance();
        cal.add(Calendar.SECOND,secs);
        return cal;
    }
    public static Calendar getCurrentTime()
    {
        Calendar cal = Calendar.getInstance();
        return cal;
    }

	public static long getThreadId()
	{
		Thread t = Thread.currentThread();
		return t.getId();
	}
	public static String getThreadSignature()
	{
		Thread t = Thread.currentThread();
		long l = t.getId();
		String name = t.getName();
		long p = t.getPriority();
		String gname = t.getThreadGroup().getName();
		return (name + ":(id)" + l + ":(priority)" + p
				+ ":(group)" + gname);
	}
	public static void logThreadSignature(String tag)
	{
		Log.d(tag, getThreadSignature());
	}
	public static void sleepForInSecs(int secs)
	{
		try
		{
			Thread.sleep(secs * 1000);
		}
		catch(InterruptedException x)
		{
			throw new RuntimeException("interrupted",x);
		}
	}

    public static List<DetectorXMLParser.Entry> loadXmlFromNetwork(String urlString) throws XmlPullParserException,IOException
    {
        InputStream stream=null;
        DetectorXMLParser detectorXMLParser=new DetectorXMLParser();
        List<DetectorXMLParser.Entry> entries=null;

        try
        {
            stream=downloadUrl(urlString);
            entries = detectorXMLParser.parse(stream);
        }
        finally {
            if(stream!=null)
            {
                stream.close();
            }
        }

        //StringBuilder sb=new StringBuilder();
        //for(Entry entry:entries)
        //{
        //    sb.append(entry.detectorID+" "+entry.detectorStatus+" "+entry.detectorDescription+"<br>");
        //}
        //return sb.toString();
        return entries;
    }
    public static String getPrefValue(Context context, String value)
    {
        SharedPreferences sharedPrefs = PreferenceManager.getDefaultSharedPreferences(context);
        return sharedPrefs.getString(value, "");
    }
    public static InputStream downloadUrl(String urlString) throws IOException
    {
        URL url=new URL(urlString);
        HttpURLConnection conn=(HttpURLConnection)url.openConnection();
        conn.setReadTimeout(10000);
        conn.setConnectTimeout(15000);
        conn.setRequestMethod("GET");
        conn.setDoInput(true);
        conn.connect();
        InputStream stream=conn.getInputStream();
        return stream;
    }
    public static String getURL(Context context) {

        SharedPreferences sharedPrefs = PreferenceManager.getDefaultSharedPreferences(context);
        String sServerAddress = sharedPrefs.getString(context.getString(R.string.pref_ServerAddress), "");
        String sServerPort = sharedPrefs.getString(context.getString(R.string.pref_ServerPort), "");
        String sInterval = sharedPrefs.getString(context.getString(R.string.pref_Interval), "");
        String sUserAccount = sharedPrefs.getString(context.getString(R.string.pref_UserAccountID), "");
        if (Utils.isStringNullOrEmpty(sServerAddress) || Utils.isStringNullOrEmpty(sServerPort) || Utils.isStringNullOrEmpty(sInterval) || Utils.isStringNullOrEmpty(sUserAccount)) {
            return "";
        } else {
            if (!sServerAddress.toLowerCase().startsWith("http://")) {
                return "http://" + sServerAddress + ":" + sServerPort + "/DetectorService/GetDetectorState/" + sUserAccount;
            } else {
                return sServerAddress + ":" + sServerPort + "/DetectorService/GetDetectorState/" + sUserAccount;
            }
        }
    }
}
