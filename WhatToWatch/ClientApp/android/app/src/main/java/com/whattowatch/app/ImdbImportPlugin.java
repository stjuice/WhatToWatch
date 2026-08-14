package com.whattowatch.app;

import android.app.Activity;
import android.content.Intent;

import androidx.activity.result.ActivityResult;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.ActivityCallback;
import com.getcapacitor.annotation.CapacitorPlugin;

/**
 * Capacitor bridge for the isolated IMDb WebView POC.
 * Opens {@link ImdbImportActivity} and returns title + __NEXT_DATA__ to JS.
 */
@CapacitorPlugin(name = "ImdbImport")
public class ImdbImportPlugin extends Plugin {

    @PluginMethod
    public void openAndExtract(PluginCall call) {
        String url = call.getString("url", ImdbImportActivity.DEFAULT_LIST_URL);

        Intent intent = new Intent(getContext(), ImdbImportActivity.class);
        intent.putExtra(ImdbImportActivity.EXTRA_URL, url);

        startActivityForResult(call, intent, "onImdbImportResult");
    }

    @ActivityCallback
    private void onImdbImportResult(PluginCall call, ActivityResult result) {
        if (call == null) {
            return;
        }

        Intent data = result.getData();
        JSObject ret = new JSObject();

        if (data != null) {
            ret.put("title", data.getStringExtra(ImdbImportActivity.EXTRA_TITLE));
            ret.put("nextData", data.getStringExtra(ImdbImportActivity.EXTRA_NEXT_DATA));
            ret.put("nextDataLength", data.getIntExtra(ImdbImportActivity.EXTRA_NEXT_DATA_LENGTH, 0));
            ret.put("error", data.getStringExtra(ImdbImportActivity.EXTRA_ERROR));
        }

        if (result.getResultCode() == Activity.RESULT_OK) {
            ret.put("ok", true);
            call.resolve(ret);
            return;
        }

        ret.put("ok", false);
        String error = ret.has("error") ? ret.getString("error") : "IMDb extract cancelled or failed";
        call.reject(error == null ? "IMDb extract cancelled or failed" : error, ret);
    }
}
