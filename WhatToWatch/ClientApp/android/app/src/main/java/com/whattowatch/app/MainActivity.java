package com.whattowatch.app;

import android.os.Bundle;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
    @Override
    public void onCreate(Bundle savedInstanceState) {
        // Isolated IMDb WebView POC plugin — remove with the POC if the experiment fails.
        registerPlugin(ImdbImportPlugin.class);
        super.onCreate(savedInstanceState);
    }
}
