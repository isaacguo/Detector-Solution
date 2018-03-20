package com.isaac.homedetector;

/**
 * Created by yueguo01 on 3/19/2015.
 */
public class DownloadXmlBoradcaseReceiver extends ALongRunningReceiver {
    @Override
    public Class getLRSClass() {
        return DownloadXmlService.class;
    }
}
