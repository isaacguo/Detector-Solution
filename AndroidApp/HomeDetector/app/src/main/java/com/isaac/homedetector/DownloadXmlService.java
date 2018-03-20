package com.isaac.homedetector;

import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.preference.PreferenceManager;

import org.xmlpull.v1.XmlPullParserException;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by yueguo01 on 3/19/2015.
 */
public class DownloadXmlService extends ALongRunningNonStickyBroadcastService {

    public DownloadXmlService()
    {
        super("com.isaac.homedetector.DownloadXmlService");
    }

    @Override
    protected void handleBroadcastIntent(Intent broadcastIntent) {
        //String urlString= broadcastIntent.getExtras().getString("urlString");
        String urlString=Utils.getURL(this);

        ArrayList<DetectorXMLParser.Entry> entries=null;
        try
        {
            entries= (ArrayList<DetectorXMLParser.Entry>)Utils.loadXmlFromNetwork(urlString);
        }catch(IOException e)
        {
            //return "读取错误，请检查网络设置";
        }
        catch(XmlPullParserException e)
        {
            //return getResources().getString(R.string.xml_error);
        }

        Intent intent=new Intent("com.isaac.homedetector.updateUI");
        intent.putExtra("list",entries);
        sendBroadcast(intent);


    }
}
