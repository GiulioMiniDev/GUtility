//using SQLite;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Text;
//using System.Threading.Tasks;

//namespace GUtility
//{
//    public class DatabaseHandler<T> where T : new()
//    {
//        public ObservableCollection<T> observable { set; get; }
//        public SQLiteAsyncConnection connection { get; set; }
//        public DatabaseHandler(string databasePath)
//        {
//            //observable = new ObservableCollection<T>();
//            connection = new SQLiteAsyncConnection(databasePath);
//        }
//        public async Task GetAllData()
//        {
//            var query = connection.Table<T>();
//            observable = new ObservableCollection<T>(await query.ToListAsync());
//        }
//        public async Task DeleteData(object pk)
//        {
//            await connection.DeleteAsync<T>(pk);
//        }
//        public async Task InsertOneElement(T element)
//        {
//            await connection.InsertAsync(element);
//        }
//        public async Task CreateTable()
//        {
//            await connection.CreateTableAsync<T>();
//        }
//    }

//}
