package com.isaac.homedetector;

import android.content.Context;
import android.graphics.Color;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;


import com.isaac.homedetector.DetectorXMLParser.Entry;

import java.util.List;

/**
 * Created by yueguo01 on 3/19/2015.
 */
public class ListDetectorsAdapter extends BaseAdapter {
    Context context;
    protected List<Entry> listDetectors;
    LayoutInflater inflater;

    public ListDetectorsAdapter(Context context, List<Entry> listDetectors)
    {
        this.listDetectors=listDetectors;
        this.context=context;
        inflater=LayoutInflater.from(context);
    }


    @Override
    public int getCount() {
        return listDetectors.size();
    }

    @Override
    public Object getItem(int position) {
        return listDetectors.get(position);
    }

    @Override
    public long getItemId(int position) {
        return position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        ViewHolder holder;
        if(convertView==null)
        {
            holder=new ViewHolder();
            convertView=this.inflater.inflate(R.layout.detector_item,parent,false);
            holder.ivDetector=(ImageView)convertView.findViewById(R.id.imgDetector);
            holder.tvDetectorDesc=(TextView)convertView.findViewById(R.id.detectorDesc);
            holder.tvDetectorStatus=(TextView)convertView.findViewById(R.id.detectorStatus);
            convertView.setTag(holder);
        }
        else
        {
            holder=(ViewHolder)convertView.getTag();
        }
        Entry detector=listDetectors.get(position);

        holder.ivDetector.setImageDrawable(context.getResources().getDrawable(R.drawable.detector));


        holder.tvDetectorDesc.setText(detector.getDetectorDescription());
        holder.tvDetectorStatus.setText(detector.getDetectorStatus());
        if(detector.getDetectorStatus().equals("Normal"))
        {
            holder.tvDetectorStatus.setText(context.getString(R.string.String_Normal_zh));
            holder.tvDetectorStatus.setTextColor(Color.parseColor("#000000"));
        }
        else
        {
            holder.tvDetectorStatus.setText(context.getString(R.string.String_Alarm_zh));
            holder.tvDetectorStatus.setTextColor(Color.parseColor("#ff0000"));
        }
//        if(detector.getDetectorStatus()!="正常")
//        {
//            holder.tvDetectorStatus.setBackgroundColor(Color.parseColor("#ff0000"));
//        }
//        else
//        {
//            holder.tvDetectorStatus.setBackgroundColor(Color.parseColor("#ffffff"));
//        }

        return convertView;
    }

    private class ViewHolder
    {
        ImageView ivDetector;
        TextView tvDetectorDesc;
        TextView tvDetectorStatus;
    }
}
