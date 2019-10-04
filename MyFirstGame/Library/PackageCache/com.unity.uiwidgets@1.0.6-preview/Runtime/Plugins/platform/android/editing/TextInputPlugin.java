// Copyright 2013 The Flutter Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

package com.unity.uiwidgets.plugin.editing;

import android.content.Context;
import android.text.Editable;
import android.text.InputType;
import android.text.Selection;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.view.inputmethod.BaseInputConnection;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputConnection;
import android.view.inputmethod.InputMethodManager;

import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import static com.unity.uiwidgets.plugin.Utils.TAG;

public class TextInputPlugin {
    private final TextInputView mView;
    private final InputMethodManager mImm;
    private int mClient = 0;
    private JSONObject mConfiguration;
    private Editable mEditable;
    private boolean mRestartInputPending;

    private static TextInputPlugin _instance;
    public static TextInputPlugin getInstance() {
        if (_instance == null) {
            _instance = new TextInputPlugin();
        }
        return _instance;
    }

    public TextInputPlugin() {
        ViewGroup contentView = (ViewGroup)UnityPlayer.currentActivity.findViewById(android.R.id.content);
        mView = new TextInputView(UnityPlayer.currentActivity);
        mView.requestFocus();
        contentView.addView(mView, 0, 0);
        mImm = (InputMethodManager) mView.getContext().getSystemService(
                Context.INPUT_METHOD_SERVICE);
    }

    public static void show() {
        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                TextInputPlugin plugin = getInstance();
                plugin.showTextInput(plugin.mView);
            }
        });
    }


    public static void hide() {
        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                TextInputPlugin plugin = getInstance();
                plugin.hideTextInput(plugin.mView);
            }
        });
    }

    public static void setClient(int client, String configurationJson) {
        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    JSONObject configuration = new JSONObject(configurationJson);
                    TextInputPlugin plugin = getInstance();
                    plugin.setTextInputClient(plugin.mView, client, configuration);
                } catch (JSONException e) {
                    Log.e(TAG, "error parse json", e);
                }
            }
        });

    }

    public static void setEditingState(String stateJson) {

        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    TextInputPlugin plugin = getInstance();
                    JSONObject state = new JSONObject(stateJson);
                    plugin.setTextInputEditingState(plugin.mView, state);
                } catch (JSONException e) {
                    Log.e(TAG, "error parse json", e);
                }
            }
        });
    }

    public static void clearClient() {

        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                TextInputPlugin plugin = getInstance();
                plugin.clearTextInputClient();
            }
        });
    }

    private static int inputTypeFromTextInputType(JSONObject type, boolean obscureText,
            boolean autocorrect, String textCapitalization) throws JSONException {
        String inputType = type.getString("name");
        if (inputType.equals("TextInputType.datetime")) return InputType.TYPE_CLASS_DATETIME;
        if (inputType.equals("TextInputType.number")) {
            int textType = InputType.TYPE_CLASS_NUMBER;
            if (type.optBoolean("signed")) textType |= InputType.TYPE_NUMBER_FLAG_SIGNED;
            if (type.optBoolean("decimal")) textType |= InputType.TYPE_NUMBER_FLAG_DECIMAL;
            return textType;
        }
        if (inputType.equals("TextInputType.phone")) return InputType.TYPE_CLASS_PHONE;

        int textType = InputType.TYPE_CLASS_TEXT;
        if (inputType.equals("TextInputType.multiline"))
            textType |= InputType.TYPE_TEXT_FLAG_MULTI_LINE;
        else if (inputType.equals("TextInputType.emailAddress"))
            textType |= InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS;
        else if (inputType.equals("TextInputType.url"))
            textType |= InputType.TYPE_TEXT_VARIATION_URI;
        if (obscureText) {
            // Note: both required. Some devices ignore TYPE_TEXT_FLAG_NO_SUGGESTIONS.
            textType |= InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS;
            textType |= InputType.TYPE_TEXT_VARIATION_PASSWORD;
        } else {
            if (autocorrect) textType |= InputType.TYPE_TEXT_FLAG_AUTO_CORRECT;
        }
        if (textCapitalization.equals("TextCapitalization.characters")) {
            textType |= InputType.TYPE_TEXT_FLAG_CAP_CHARACTERS;
        } else if (textCapitalization.equals("TextCapitalization.words")) {
            textType |= InputType.TYPE_TEXT_FLAG_CAP_WORDS;
        } else if (textCapitalization.equals("TextCapitalization.sentences")) {
            textType |= InputType.TYPE_TEXT_FLAG_CAP_SENTENCES;
        }
        return textType;
    }

    private static int inputActionFromTextInputAction(String inputAction) {
        switch (inputAction) {
            case "TextInputAction.newline":
                return EditorInfo.IME_ACTION_NONE;
            case "TextInputAction.none":
                return EditorInfo.IME_ACTION_NONE;
            case "TextInputAction.unspecified":
                return EditorInfo.IME_ACTION_UNSPECIFIED;
            case "TextInputAction.done":
                return EditorInfo.IME_ACTION_DONE;
            case "TextInputAction.go":
                return EditorInfo.IME_ACTION_GO;
            case "TextInputAction.search":
                return EditorInfo.IME_ACTION_SEARCH;
            case "TextInputAction.send":
                return EditorInfo.IME_ACTION_SEND;
            case "TextInputAction.next":
                return EditorInfo.IME_ACTION_NEXT;
            case "TextInputAction.previous":
                return EditorInfo.IME_ACTION_PREVIOUS;
            default:
                // Present default key if bad input type is given.
                return EditorInfo.IME_ACTION_UNSPECIFIED;
        }
    }

    public InputConnection createInputConnection(View view, EditorInfo outAttrs)
            throws JSONException {
        if (mClient == 0) return null;

        outAttrs.inputType = inputTypeFromTextInputType(mConfiguration.getJSONObject("inputType"),
                mConfiguration.optBoolean("obscureText"),
                mConfiguration.optBoolean("autocorrect", true),
                mConfiguration.getString("textCapitalization"));
        outAttrs.imeOptions = EditorInfo.IME_FLAG_NO_FULLSCREEN;
        int enterAction;
        if (mConfiguration.isNull("inputAction")) {
            // If an explicit input action isn't set, then default to none for multi-line fields
            // and done for single line fields.
            enterAction = (InputType.TYPE_TEXT_FLAG_MULTI_LINE & outAttrs.inputType) != 0
                    ? EditorInfo.IME_ACTION_NONE
                    : EditorInfo.IME_ACTION_DONE;
        } else {
            enterAction = inputActionFromTextInputAction(mConfiguration.getString("inputAction"));
        }
        if (!mConfiguration.isNull("actionLabel")) {
            outAttrs.actionLabel = mConfiguration.getString("actionLabel");
            outAttrs.actionId = enterAction;
        }
        outAttrs.imeOptions |= enterAction;

        InputConnectionAdaptor connection =
                new InputConnectionAdaptor(view, mClient, mEditable);
        outAttrs.initialSelStart = Selection.getSelectionStart(mEditable);
        outAttrs.initialSelEnd = Selection.getSelectionEnd(mEditable);

        return connection;
    }

    private void showTextInput(View view) {
        view.requestFocus();
        mImm.showSoftInput(view, 0);
    }

    private void hideTextInput(View view) {
        mImm.hideSoftInputFromWindow(view.getApplicationWindowToken(), 0);
    }

    private void setTextInputClient(View view, int client, JSONObject configuration) {
        mClient = client;
        mConfiguration = configuration;
        mEditable = Editable.Factory.getInstance().newEditable("");

        // setTextInputClient will be followed by a call to setTextInputEditingState.
        // Do a restartInput at that time.
        mRestartInputPending = true;
    }

    private void applyStateToSelection(JSONObject state) throws JSONException {
        int selStart = state.getInt("selectionBase");
        int selEnd = state.getInt("selectionExtent");
        if (selStart >= 0 && selStart <= mEditable.length() && selEnd >= 0
                && selEnd <= mEditable.length()) {
            Selection.setSelection(mEditable, selStart, selEnd);
        } else {
            Selection.removeSelection(mEditable);
        }
    }

    private void setTextInputEditingState(View view, JSONObject state) throws JSONException {
        if (!mRestartInputPending && state.getString("text").equals(mEditable.toString())) {
            applyStateToSelection(state);
            mImm.updateSelection(mView, Math.max(Selection.getSelectionStart(mEditable), 0),
                    Math.max(Selection.getSelectionEnd(mEditable), 0),
                    BaseInputConnection.getComposingSpanStart(mEditable),
                    BaseInputConnection.getComposingSpanEnd(mEditable));
        } else {
            mEditable.replace(0, mEditable.length(), state.getString("text"));
            applyStateToSelection(state);
            mImm.restartInput(view);
            mRestartInputPending = false;
        }
    }

    private void clearTextInputClient() {
        mClient = 0;
    }
}
