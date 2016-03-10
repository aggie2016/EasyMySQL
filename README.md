# EasyMySQL
EasyMySQL is a wrapper for the .NET MySQL connector that simplifies the data storage and retrieval process by saving and retrieving entire user-defined class types. This removes the need to manually get each field in a query column and should speed up the development process. Please note to use this libray, a reference to the MySQL.Data library is required (>v6.9.6.0)

Additional documentation for this library will be posted as it's development progresses!


##How to use this library
(1) Create a class that will hold all primitive and user-defined types and ensure the class is marked as serializable. Any additional nested classes will also need to be marked as serializable.

(2) Create a MySQLConnection to later pass to the EasyMySQL initializer

(3) Create an instance of the EasyMySQL class in your program. This will provide all the methods for storage and retrieval of your data to/from the MySQL server

(4) ONE-TIME ONLY: Call the following EasyMySQL method and provide both a class type and name for the table to create
```C#
    CreateTable<T>(string tableName)
    
    EXAMPLE:
    MySqlConnection conn = new MySqlConnection("Your connection string here");
    EasyMySQL SQLManager = new EasyMySQL(conn);
    SQLManager.CreateTable<TestClass2>("test_table");
```

(5) Add rows to the table by calling the following EasyMySQL method. Provide the class type, table name used earlier, and the object to save. Please note that a single table can only be used with the class it was created for.

```C#
    SaveToDatabase<T>(string tableName, T objectToSave)
    
    EXAMPLE:
    
    TestClass2 newclass = new TestClass2();
            newclass.id = 2;
            newclass.mybool = true;
            newclass.name = "Chris";
            newclass.newdate = DateTime.Parse("1/21/2016");
            newclass.numberofnames = 3;
            newclass.nestedclass.value1 = "this test class";
            newclass.nestedclass.value2 = "will be converted to bytes";
            newclass.nestedclass.value3 = "and saved as a LONGBLOB";
    
    MySqlConnection conn = new MySqlConnection("Your connection string here");
    EasyMySQL SQLManager = new EasyMySQL(conn);
    SQLManager.SaveToDatabase<TestClass2>("test_table", newclass)

```

(6) Retrieve data from the created table by calling one of two overloads for the following method. The "Filter" class is built in and allows narrowing of search criteria by specifying the filter (field name from class) and criteria (value) to use. Including the Filter appends a "WHERE" term to the query string.

```C#
    RetrieveFromDatabase<T>(string tableName, int startindex, int count);
    RetrieveFromDatabase<T>(string tableName, int startindex, int count, Filter filter);
    
    EXAMPLE:
    
    MySqlConnection conn = new MySqlConnection("Your connection string here");
    EasyMySQL SQLManager = new EasyMySQL(conn);
    List<TestClass2> objectsfound = SQLManager.RetrieveFromDatabase<TestClass2>("test_table", 0, 10, new Filter("id", 2));
```


##Full Example

```C#

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySQLTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //Sample class for use with this library
            TestClass2 newclass = new TestClass2();
            newclass.id = 2;
            newclass.mybool = true;
            newclass.name = "Chris";
            newclass.newdate = DateTime.Parse("1/21/2016");
            newclass.numberofnames = 3;
            newclass.nestedclass.value1 = "this test class";
            newclass.nestedclass.value2 = "will be converted to bytes";
            newclass.nestedclass.value3 = "and saved as a LONGBLOB";

            //Create a MySQL connections string
            MySqlConnection conn = new MySqlConnection("Your connection string here");

            //Pass this to an instance of the EasyMySQL class
            EasyMySQL SQLManager = new EasyMySQL(conn);

            //Create a new table for the class we will be saving
            if (SQLManager.CreateTable<TestClass2>("test_table") == SQLRESULT.Success)
            {
                Console.WriteLine("Table created successfully!");
            }
            else
            {
                Console.WriteLine("Table creation failed: " + SQLmanager.mysqlerror);
            }

            //Save an instance of our custom class to the table we created for it
            if (SQLManager.SaveToDatabase<TestClass2>("test_table", newclass) == SQLRESULT.Success)
                Console.WriteLine("Success saving class to testtable");
            else {
                Console.WriteLine("EXCEPTION THROWN: " + SQLmanager.mysqlerror);
            }


            //Retrieve a list of the first 10 rows in the "testtable" containing instances of our custom class. 
            //Additionally provide a "Filter(string filterfield, object filtervalue)" that appends a WHERE statement to the end of the query
            List<TestClass2> objectsfound = SQLManager.RetrieveFromDatabase<TestClass2>("test_table", 0, 10, new Filter("id", 2));

            Console.ReadLine();
        }
    }



    [Serializable]
    class TestClass2
    {
        public TestClass2()
        {
            id = 0;
            name = "";
            newdate = new DateTime();
            mybool = false;
            numberofnames = 0;
            nestedclass = new TestClass3();
        }
        public int id { get; set; }
        public string name { get; set; }
        public DateTime newdate { get; set; }
        public bool mybool { get; set; }
        public double numberofnames { get; set; }
        public TestClass3 nestedclass { get; set; }
    }

    [Serializable]
    class TestClass3
    {
        public TestClass3()
        {
            value1 = "";
            value1 = "";
            value3 = "";
        }
        public string value1 { get; set; }
        public string value2 { get; set; }
        public string value3 { get; set; }
    }
}
```
