package com.plutinosoft.platinum.sample;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;

import com.plutinosoft.platinum.UPnP;

public class PlatinumUPnPActivity extends Activity {
    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);
        
        startStopButton = (Button)findViewById(R.id.startStopButton);
        startStopButton.setEnabled(true);
        
        upnp = new UPnP();
    }
    
    public void onStartStopButtonClicked(View button) {
        if (isRunning) {
            upnp.stop();
            isRunning = false;
            startStopButton.setText("Start`");
        } else {
            int result = upnp.start();
            Log.d(TAG, "upnp.Start returned: " + result);
            if (result == 0) {
                isRunning = true;
                startStopButton.setText("Stop");
            }
        }
    }

    private final String TAG = PlatinumUPnPActivity.this.getClass().getName();
    private UPnP  upnp;
    private boolean isRunning;
    
    private Button   startStopButton;
}