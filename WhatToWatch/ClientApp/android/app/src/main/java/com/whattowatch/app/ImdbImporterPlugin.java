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

import org.json.JSONException;
import org.json.JSONObject;

/**
 * Bridges the React app to the internal IMDb WebView import screen.
 *
 * <pre>await ImdbImporter.importList({ url })</pre>
 */
@CapacitorPlugin(name = "ImdbImporter")
public class ImdbImporterPlugin extends Plugin {

    @PluginMethod
    public void importList(PluginCall call) {
        String url = call.getString("url");
        if (url == null || url.trim().isEmpty()) {
            call.reject("A list url is required");
            return;
        }

        Intent intent = new Intent(getContext(), ImdbImportActivity.class);
        intent.putExtra(ImdbImportActivity.EXTRA_URL, url.trim());

        startActivityForResult(call, intent, "onImportResult");
    }

    @ActivityCallback
    private void onImportResult(PluginCall call, ActivityResult result) {
        if (call == null) {
            return;
        }

        Intent data = result.getData();

        if (result.getResultCode() != Activity.RESULT_OK) {
            String error = data == null ? null : data.getStringExtra(ImdbImportActivity.EXTRA_ERROR);
            call.reject(error == null ? ImdbImportActivity.ERROR_CANCELLED : error);
            return;
        }

        String json = data == null ? null : data.getStringExtra(ImdbImportActivity.EXTRA_WATCHLIST_JSON);
        if (json == null) {
            call.reject("The IMDb screen returned no watchlist data");
            return;
        }

        try {
            call.resolve(JSObject.fromJSONObject(new JSONObject(json)));
        } catch (JSONException e) {
            call.reject("Could not parse the imported watchlist: " + e.getMessage());
        }
    }
}
