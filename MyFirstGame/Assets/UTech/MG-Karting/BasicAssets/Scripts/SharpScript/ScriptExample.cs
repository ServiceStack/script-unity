﻿using System;
using System.IO;
using ServiceStack.Script;
using UnityEngine;
using UnityEngine.UI;

public class ScriptExample : MonoBehaviour
{
    private ScriptContext script;

    /// <summary>
    /// Typed API wrappers required for some of Unity's "special properties"
    /// </summary>
    public class UnityScripts : ScriptMethods
    {
        public string name(GameObject o, string name) => o.name = name; 
        public Transform transform(Component c) => c.transform;
        public Color color(Material m, Color color) { m.SetColor("_Color", color); return color; }
        public float mass(Rigidbody rb, float value) => rb.mass = value;
        public Vector3 position(Transform t) => t.position;
        public Vector3 position(Transform t, Vector3 position) => t.position = position;
        public Vector3 localScale(Transform t) => t.localScale;
        public Vector3 localScale(Transform t, Vector3 localScale) => t.localScale = localScale;
    }

    Lisp.Interpreter lisp;
    InputField txtRepl;
    Text textReplOut;

    // Start is called before the first frame update
    void Start()
    {
        script = new ScriptContext {
            ScriptLanguages = {
                ScriptLisp.Language  
            },
            ScriptMethods = {
                new ProtectedScripts(),  
                new UnityScripts(),
            },
            AllowScriptingOfAllTypes = true,
            ScriptNamespaces = {
                nameof(UnityEngine)
            },
            Args = {
                [nameof(gameObject)] = gameObject,
            }
        }.Init();
        
        lisp = Lisp.CreateInterpreter();

        txtRepl = gameObject.GetComponentInChildren<InputField>();
        textReplOut = gameObject.GetComponentInChildren<Text>();
    }

    private string lastScript = "";

    // Update is called once per frame
    void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.Return))
        {
            var srcLisp = txtRepl.text;
            if (srcLisp == lastScript) // prevent multiple evals
                return;
            
            lastScript = srcLisp;
            try
            {
                var output = lisp.ReplEval(script, Stream.Null, srcLisp);
                textReplOut.color = Color.white;
                textReplOut.text = output;
            }
            catch (Exception e)
            {
                textReplOut.color = Color.red;
                textReplOut.text = e.ToString();
            }
            txtRepl.Select();
            txtRepl.ActivateInputField();
        }
    }
}