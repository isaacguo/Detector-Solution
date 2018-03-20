package com.isaac.homedetector;

import android.os.Parcel;
import android.os.Parcelable;
import android.util.Xml;

import org.xmlpull.v1.XmlPullParser;
import org.xmlpull.v1.XmlPullParserException;

import java.io.IOException;
import java.io.InputStream;
import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by Isaac on 3/18/2015.
 */
public class DetectorXMLParser {
    private static final String ns = null;

    public List<Entry> parse(InputStream in) throws XmlPullParserException, IOException {
        try {
            XmlPullParser parser = Xml.newPullParser();
            parser.setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES, false);
            parser.setInput(in, null);
            parser.nextTag();
            return readFeed(parser);
        } finally {
            in.close();
        }
    }

    private List<Entry> readFeed(XmlPullParser parser) throws XmlPullParserException, IOException {
        List<Entry> entries = new ArrayList<Entry>();
        parser.require(XmlPullParser.START_TAG, ns, "ArrayOfDetector");
        while (parser.next() != XmlPullParser.END_TAG) {
            if (parser.getEventType() != XmlPullParser.START_TAG) {
                continue;

            }
            String name = parser.getName();
            if (name.equals("Detector")) {
                entries.add(readEntry(parser));
            } else {
                skip(parser);
            }
        }
        return entries;
    }

    private Entry readEntry(XmlPullParser parser) throws XmlPullParserException, IOException {
        parser.require(XmlPullParser.START_TAG, ns, "Detector");
        String detectorID = null;
        String detectorStatus = null;
        String detectorDescription = null;
        while (parser.next() != XmlPullParser.END_TAG) {
            if (parser.getEventType() != XmlPullParser.START_TAG) {
                continue;
            }
            String name = parser.getName();
            if (name.equals("DetectorDescription")) {
                detectorDescription= readTextValue(parser,"DetectorDescription");
            }else if(name.equals("DetectorID")) {
                detectorID=readTextValue(parser,"DetectorID");
            }else if(name.equals("Status")) {
                //detectorStatus=readTextValue(parser,"Status").equals("Normal")?"正常":"报警";
                detectorStatus=readTextValue(parser,"Status");
            }
            else
                skip(parser);
        }
        return new Entry(detectorID,detectorStatus,detectorDescription);
    }
        private String readTextValue(XmlPullParser parser, String field) throws IOException,XmlPullParserException
    {
        parser.require(XmlPullParser.START_TAG,ns,field);
        String text=readText(parser);
        parser.require(XmlPullParser.END_TAG,ns,field);
        return text;

    }
    private String readText(XmlPullParser parser) throws IOException,XmlPullParserException
    {
        String result="";
        if(parser.next()==XmlPullParser.TEXT)
        {
            result=parser.getText();
            parser.nextTag();
        }
        return result;
    }
    private void skip(XmlPullParser parser) throws XmlPullParserException, IOException
    {
        if(parser.getEventType()!=XmlPullParser.START_TAG)
        {
            throw new IllegalStateException();
        }
        int depth=1;
        while (depth!=0)
        {
            switch (parser.next())
            {
                case XmlPullParser.END_TAG:
                    depth--;
                    break;
                case XmlPullParser.START_TAG:
                    depth++;
                    break;
            }
        }
    }









    public static class Entry implements Serializable
    {
        public final String detectorID;
        public final String detectorStatus;
        public String getDetectorStatus()
        {
            return detectorStatus;
        }


        public final String detectorDescription;
        public String getDetectorDescription()
        {
            return detectorDescription;
        }


        private Entry(String detectorID,String detectorStatus,String detectorDescription)
        {
            this.detectorID=detectorID;
            this.detectorStatus=detectorStatus;
            this.detectorDescription=detectorDescription;
        }




    }

}
