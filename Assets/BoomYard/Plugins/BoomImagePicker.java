package com.boomyard.picker;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;

/**
 * Android: picks a picture for a stencil with the system picker (no storage permission needed), scales it down
 * to 256 px and hands the game a PNG path (ImagePicker.OnPickedFile). A headless fragment receives the result.
 */
@SuppressWarnings("deprecation")
public class BoomImagePicker extends Fragment {
    static final int REQUEST = 7071;
    static String target;

    public static void pick(String gameObject) {
        target = gameObject;
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            public void run() {
                activity.getFragmentManager().beginTransaction().add(new BoomImagePicker(), "boomyard_picker").commitAllowingStateLoss();
            }
        });
    }

    @Override
    public void onCreate(Bundle state) {
        super.onCreate(state);
        Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
        intent.setType("image/*");
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        try {
            startActivityForResult(Intent.createChooser(intent, "Choose a picture"), REQUEST);
        } catch (Exception e) {
            send("OnPickFailed", "");
            remove();
        }
    }

    @Override
    public void onActivityResult(int request, int result, Intent data) {
        if (request != REQUEST) return;
        if (result != Activity.RESULT_OK || data == null || data.getData() == null) {
            send("OnPickCancelled", "");
        } else {
            try {
                String path = shrink(data.getData());
                if (path != null) send("OnPickedFile", path); else send("OnPickFailed", "");
            } catch (Exception e) {
                send("OnPickFailed", "");
            }
        }
        remove();
    }

    String shrink(Uri uri) throws IOException {
        Activity activity = getActivity();
        BitmapFactory.Options bounds = new BitmapFactory.Options();
        bounds.inJustDecodeBounds = true;
        InputStream in = activity.getContentResolver().openInputStream(uri);
        BitmapFactory.decodeStream(in, null, bounds);
        in.close();
        int sample = 1;
        while (Math.max(bounds.outWidth, bounds.outHeight) / (sample * 2) >= 256) sample *= 2;
        BitmapFactory.Options options = new BitmapFactory.Options();
        options.inSampleSize = sample;
        in = activity.getContentResolver().openInputStream(uri);
        Bitmap bmp = BitmapFactory.decodeStream(in, null, options);
        in.close();
        if (bmp == null) return null;
        float k = Math.min(1f, 256f / Math.max(bmp.getWidth(), bmp.getHeight()));
        if (k < 1f) bmp = Bitmap.createScaledBitmap(bmp, Math.max(1, Math.round(bmp.getWidth() * k)), Math.max(1, Math.round(bmp.getHeight() * k)), true);
        File file = new File(activity.getCacheDir(), "boomyard_pick.png");
        FileOutputStream out = new FileOutputStream(file);
        bmp.compress(Bitmap.CompressFormat.PNG, 100, out);
        out.close();
        return file.getAbsolutePath();
    }

    static void send(String method, String value) {
        UnityPlayer.UnitySendMessage(target, method, value);
    }

    void remove() {
        try {
            getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
        } catch (Exception e) {
            // the activity is going away: nothing to clean up
        }
    }
}
