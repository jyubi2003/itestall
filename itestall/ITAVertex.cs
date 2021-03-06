﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace itestall
{
    class ITAVertex
    {
        // 頂点のカテゴリ（NODE, TOKEN, TRIVIA, NONE)
        String category;
        // 頂点のタイプ
        String type;
        // 頂点の種別
        String kind;
        // プロパティのコレクション
        SortedList<String, String> props;

        // コンストラクタ
        public ITAVertex()
        {
            category = "";
            type = "";
            kind = "";
            props = new SortedList<String, String>();
        }

        // コンストラクタ
        public ITAVertex(String aCategory, String aType, String aKind)
        {
            category = aCategory;
            type = aType;
            kind = aKind;
            props = new SortedList<String, String>();
        }

        // Kindの設定
        public void SetCategory(String aCategory)
        {
            category = aCategory;
        }

        // Typeの設定
        public void SetType(String aType)
        {
            type = aType;
        }

        // Kindの設定
        public void SetKind(String aKind)
        {
            kind = aKind;
        }

        // Category,Type,Kindの設定
        public void SetCategoryTypeAndKind(String aCategory, String aType, String aKind)
        {
            category = aCategory;
            type = aType;
            kind = aKind;
        }

        // プロパティの設定
        public void SetProperty(String aKey, String aValue)
        {
            props.Add(aKey, aValue);
        }

        // プロパティの設定
        public String GetProperty(String aKey)
        {
            String value;
            props.TryGetValue(aKey, out value);
            return value;
        }

        public override String ToString()
        {
            // 全てのプロパティを出力する
            String RepProp = category + "." + type + "." + kind +"\n ";
            foreach (KeyValuePair<string, string> pair in props) {
                if (pair.Key != "SyntaxTree") {
                    RepProp += pair.Key + "=" + pair.Value + "\n ";
                }
            }
            RepProp += "\n";

            return RepProp;
        }
    }
}
