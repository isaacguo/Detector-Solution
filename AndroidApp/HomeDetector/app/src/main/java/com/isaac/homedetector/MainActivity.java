package com.isaac.homedetector;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.media.AudioManager;
import android.media.MediaPlayer;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.preference.PreferenceManager;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.webkit.WebView;
import android.widget.Button;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;
import android.os.AsyncTask;

import org.xmlpull.v1.XmlPullParserException;
import com.isaac.homedetector.DetectorXMLParser.Entry;

import java.io.IOException;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;


public class MainActivity extends ActionBarActivity {

    public static final String WIFI="Wi-Fi";
    public static final String ANY="Any";

    private static boolean wifiConnected=false;
    private static boolean mobileConnected=false;
    public static boolean refreshDisplay=true;
    public static String sPref=null;
    private boolean isAlarmSet=false;
    private static boolean isCancelSoundAlarmButtonPressed=false;
    private boolean isPlayingAlarm=false;
    private MediaPlayer mediaPlayer;



    private NetworkReceiver receiver=new NetworkReceiver();
    private UpdateControlsReceiver ucReceiver=new UpdateControlsReceiver();
    private ResetIsCancelSoundAlarmButtonPressedStateReceiver csReceiver=new ResetIsCancelSoundAlarmButtonPressedStateReceiver();


    ListView listDetectors;
    TextView tvConnectionStatus;
    Button btnCancelAlarm;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        UpdateViews(this,null);


        IntentFilter filter=new IntentFilter(ConnectivityManager.CONNECTIVITY_ACTION);
        receiver=new NetworkReceiver();
        this.registerReceiver(receiver,filter);
        IntentFilter filter2=new IntentFilter("com.isaac.homedetector.updateUI");
        this.registerReceiver(ucReceiver,filter2);
        IntentFilter filter3=new IntentFilter("com.isaac.homedetector.updateCancelButton");
        this.registerReceiver(csReceiver,filter3);
    }

    @Override
    protected void onStop() {
        super.onStop();

    }

    @Override
    protected void onPause() {
        super.onPause();
        cancelRepeatingAlarm();
        stopLocalAudio();
    }

    @Override
    protected void onStart() {
        super.onStart();

        SharedPreferences sharedPrefs = PreferenceManager.getDefaultSharedPreferences(this);
        sPref = sharedPrefs.getString("listPref", "Wi-Fi");

        String urlString = Utils.getURL(this);
        if (!Utils.isStringNullOrEmpty(Utils.getURL(this))) {
            int interval = 60;
            String prefInterval=Utils.getPrefValue(this,"pref_Interval");
            if (prefInterval != null) {
                interval = Integer.parseInt(prefInterval);
                if(interval>0)
                {
                    sendRepeatingAlarm(interval, urlString);
                }
            }
        }


        updateConnectedFlags();
        if (refreshDisplay) {
            loadPage();
        }
    }

    private void loadPage() {
        String urlString=Utils.getURL(this);
        if(Utils.isStringNullOrEmpty(urlString)) return;
        if (((sPref.equals(ANY)) && (wifiConnected || mobileConnected))
                || ((sPref.equals(WIFI)) && (wifiConnected))) {
            // AsyncTask subclass
            //String URL=sServerAddress+":"+sServerPort+"/DetectorService/GetDetectorState/1";
            new DownloadXmlTask(this).execute(urlString);
        } else {
            showErrorPage();
        }
    }


    private void showErrorPage()
    {
        //setContentView(R.layout.activity_main);

    }
    private void updateConnectedFlags() {
        ConnectivityManager connMgr=(ConnectivityManager)getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo activeInfo=connMgr.getActiveNetworkInfo();
        if(activeInfo!=null && activeInfo.isConnected())
        {
            wifiConnected=activeInfo.getType()==ConnectivityManager.TYPE_WIFI;
            mobileConnected=activeInfo.getType()==ConnectivityManager.TYPE_MOBILE;
        }
        else
        {
            wifiConnected=false;
            mobileConnected=false;
        }
    }

    public void doClick(View view)
    {
        Calendar cal= Utils.getTimeAfterInSecs(15);
        isCancelSoundAlarmButtonPressed=true;
        Intent intent=new Intent("com.isaac.homedetector.updateCancelButton");
        PendingIntent pi=getDistinctPendingIntent(intent,2);
        AlarmManager am=(AlarmManager)getSystemService(Context.ALARM_SERVICE);
        am.set(AlarmManager.RTC,cal.getTimeInMillis(),pi);

        if(isPlayingAlarm)
        {
            stopLocalAudio();
        }
        btnCancelAlarm.setVisibility(View.INVISIBLE);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        switch (id)
        {
            case R.id.action_settings:
                Intent settingsActivity=new Intent(getBaseContext(),SettingsActivity.class);
                startActivity(settingsActivity);
                return true;
            default:
                return super.onOptionsItemSelected(item);
        }
    }

    public void sendRepeatingAlarm(int interval,String urlString)
    {
        Calendar cal=Utils.getTimeAfterInSecs(interval);
        Intent intent=new Intent("com.isaac.intents.loadxmldata");
        intent.putExtra("urlString",urlString);
        PendingIntent pi=getDistinctPendingIntent(intent,1);
        AlarmManager am=(AlarmManager)getSystemService(Context.ALARM_SERVICE);
        if(isAlarmSet)
        {
            cancelRepeatingAlarm();
        }
        am.setRepeating(AlarmManager.RTC,cal.getTimeInMillis(),interval*1000,pi);
        isAlarmSet=true;
    }
    public void cancelRepeatingAlarm()
    {
        Intent intent=new Intent("com.isaac.intents.loadxmldata");
        PendingIntent pi=getDistinctPendingIntent(intent,1);
        AlarmManager am=(AlarmManager)getSystemService(Context.ALARM_SERVICE);
        am.cancel(pi);
        isAlarmSet=false;
    }

    protected PendingIntent getDistinctPendingIntent(Intent intent,int requestId)
    {
        PendingIntent pi=
                PendingIntent.getBroadcast(this,requestId,intent,0);
        return pi;
    }


    public class NetworkReceiver extends BroadcastReceiver{

        @Override
        public void onReceive(Context context, Intent intent) {
            ConnectivityManager connMgr=(ConnectivityManager)context.getSystemService(Context.CONNECTIVITY_SERVICE);
            NetworkInfo networkInfo=connMgr.getActiveNetworkInfo();
           if(WIFI.equals(sPref) && networkInfo!=null && networkInfo.getType()==ConnectivityManager.TYPE_WIFI)
           {
               refreshDisplay=true;
               Toast.makeText(context,"WIFI 已连接",Toast.LENGTH_SHORT).show();
           }
            else if(ANY.equals(sPref) && networkInfo!=null)
           {
               refreshDisplay=true;
           }
            else
           {
               refreshDisplay=false;
               Toast.makeText(context,"失去连接",Toast.LENGTH_SHORT).show();
           }
        }
    }


    private class DownloadXmlTask extends AsyncTask<String,Void,List<Entry> >
    {
        private Context context;
        DownloadXmlTask(Context context)
        {
            this.context=context;
        }
        @Override
        protected List<Entry> doInBackground(String... urls) {
            try
            {
                return Utils.loadXmlFromNetwork(urls[0]);
            }catch(IOException e)
            {
                //return "读取错误，请检查网络设置";
                return null;
            }
            catch(XmlPullParserException e)
            {
                //return getResources().getString(R.string.xml_error);
                return null;
            }
        }

        @Override
        protected void onPostExecute(List<Entry> result) {
            UpdateViews(context,result);
            //WebView myWebView=(WebView)findViewById(R.id.webview);
            //myWebView.getSettings().setDefaultTextEncodingName("utf-8");
            //myWebView.loadData(result,"text/html; charset=UTF-8",null);

        }
    }

    private void UpdateViews(Context context, List<Entry> result)
    {
        setContentView(R.layout.activity_main);
        btnCancelAlarm=(Button)findViewById(R.id.btnCancelAlarm);
        btnCancelAlarm.setVisibility(View.INVISIBLE);
        listDetectors=(ListView)findViewById(R.id.detectorList);
        tvConnectionStatus=(TextView)findViewById(R.id.connectionStatus);

        if(result!=null)
        {
            tvConnectionStatus.setText("");
            ListDetectorsAdapter adapter=new ListDetectorsAdapter(context,result);
            listDetectors.setAdapter(adapter);
        }
        else
        {
            tvConnectionStatus.setText("读取错误，请检查网络设置");
            listDetectors.setAdapter(null);
        }
    }

    public void playLocalAudio() throws Exception
    {
        mediaPlayer= MediaPlayer.create(this,R.raw.alarm);
        mediaPlayer.setAudioStreamType(AudioManager.STREAM_MUSIC);
        mediaPlayer.setLooping(true);
        mediaPlayer.start();
    }
    public void stopLocalAudio()
    {
        if(mediaPlayer!=null)
        {
            mediaPlayer.stop();
        }
        isPlayingAlarm=false;
    }



    public class UpdateControlsReceiver extends BroadcastReceiver
    {


        @Override
        public void onReceive(Context context, Intent intent) {
            boolean inAbnormalState=false;
            List<Entry> entries =(ArrayList<Entry>)intent.getSerializableExtra("list");
            if (entries==null) return;
            UpdateViews(getApplicationContext(),entries);
            for(Entry entry:entries)
            {
                if(!entry.getDetectorStatus().equals("Normal"))
                {
                    inAbnormalState=true;
                }
            }

            if(!inAbnormalState && isPlayingAlarm)
            {
                stopLocalAudio();
            }
            if(inAbnormalState && !isPlayingAlarm && !isCancelSoundAlarmButtonPressed)
            {
                try
                {
                    playLocalAudio();
                    isPlayingAlarm=true;
                }
                catch (Exception e)
                {

                }
            }
            if(inAbnormalState && isPlayingAlarm && isCancelSoundAlarmButtonPressed)
            {
                btnCancelAlarm.setVisibility(View.INVISIBLE);
            }
            if(inAbnormalState && isPlayingAlarm && !isCancelSoundAlarmButtonPressed)
            {
                btnCancelAlarm.setVisibility(View.VISIBLE);
            }


        }
    }
    public class ResetIsCancelSoundAlarmButtonPressedStateReceiver extends BroadcastReceiver
    {
        @Override
        public void onReceive(Context context, Intent intent) {
            isCancelSoundAlarmButtonPressed=false;

        }
    }
}
