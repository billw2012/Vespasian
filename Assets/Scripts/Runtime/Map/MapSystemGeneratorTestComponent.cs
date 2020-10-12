using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class MapSystemGeneratorTestComponent : MonoBehaviour
{
    public BodySpecs bodySpecs;
    public MapGenerator generator;

    void Start()
    {
        this.dataHash = HashObject(this.bodySpecs) + HashObject(this.generator);
        this.Generate();
    }

    int key;
    string dataHash;

    public void Generate()
    {
        this.key = (int)(DateTime.Now.Ticks % int.MaxValue);
        this.RegenerateAsync();
    }

    public async void RegenerateAsync()
    {
        var system = this.generator.GenerateSystem(this.key, this.bodySpecs, "test", Vector2.zero);
        await system.LoadAsync(this.bodySpecs, this.gameObject);
        FindObjectOfType<SimManager>().Refresh();
    }

    static string HashObject(object o)
    {
        using (var sha256Hash = SHA256.Create())
        {
            using (var ms = new MemoryStream())
            {
                var bf = new DataContractSerializer(o.GetType());
                bf.WriteObject(ms, o);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] data = sha256Hash.ComputeHash(ms);
                var sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }

    public void Update()
    {
        string newHash = HashObject(this.bodySpecs) + HashObject(this.generator);
        if (newHash != this.dataHash)
        {
            this.dataHash = newHash;
            this.RegenerateAsync();
        }
    }
}
