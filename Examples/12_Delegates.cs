/*
 * Copyright (c) 2009, Stefan Simek
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */

using System;
using System.Collections;
using System.Text;

namespace TriAxis.RunSharp.Examples
{
	class _12_Delegates
	{
		// example based on the MSDN Delegates Sample (bookstore.cs)
		public static void GenBookstore(AssemblyGen ag)
		{
			TypeGen Book, ProcessBookDelegate, BookDB;

			// A set of classes for handling a bookstore:
			using (ag.Namespace("Bookstore"))
			{
				// Describes a book in the book list:
				Book = ag.Public.Struct("Book");
				{
					FieldGen Title = Book.Public.Field<string>("Title");	   // Title of the book.
					FieldGen Author = Book.Public.Field<string>("Author");     // Author of the book.
					FieldGen Price = Book.Public.Field<decimal>("Price");      // Price of the book.
					FieldGen Paperback = Book.Public.Field<bool>("Paperback"); // Is it paperback?

					CodeGen g = Book.Public.Constructor()
						.Parameter<string>("title")
						.Parameter<string>("author")
						.Parameter<decimal>("price")
						.Parameter<bool>("paperBack");
					{
						g.Assign(Title, g.Arg("title"));
						g.Assign(Author, g.Arg("author"));
						g.Assign(Price, g.Arg("price"));
						g.Assign(Paperback, g.Arg("paperBack"));
					}
				}

				// Declare a delegate type for processing a book:
				ProcessBookDelegate = ag.Public.DelegateVoid("ProcessBookDelegate").Parameter(Book, "book");

				// Maintains a book database.
				BookDB = ag.Public.Class("BookDB");
				{
					// List of all books in the database:
					FieldGen list = BookDB.Field<ArrayList>("list", Exp.New<ArrayList>());

					// Add a book to the database:
					CodeGen g = BookDB.Public.Void("AddBook")
						.Parameter<string>("title")
						.Parameter<string>("author")
						.Parameter<decimal>("price")
						.Parameter<bool>("paperBack");
					{
						g.Invoke(list, "Add", Exp.New(Book, g.Arg("title"), g.Arg("author"), g.Arg("price"), g.Arg("paperBack")));
					}

					// Call a passed-in delegate on each paperback book to process it: 
					g = BookDB.Public.Void("ProcessPaperbackBooks").Parameter(ProcessBookDelegate, "processBook");
					{
						Operand b = g.ForEach(Book, list);
						{
							g.If(b.Field("Paperback"));
							{
								g.InvokeDelegate(g.Arg("processBook"), b);
							}
							g.End();
						}
						g.End();
					}
				}
			}

			// Using the Bookstore classes:
			using (ag.Namespace("BookTestClient"))
			{
				// Class to total and average prices of books:
				TypeGen PriceTotaller = ag.Class("PriceTotaller");
				{
					FieldGen countBooks = PriceTotaller.Field<int>("countBooks", 0);
					FieldGen priceBooks = PriceTotaller.Field<decimal>("priceBooks", 0.0m);

					CodeGen g = PriceTotaller.Internal.Void("AddBookToTotal").Parameter(Book, "book");
					{
						g.AssignAdd(countBooks, 1);
						g.AssignAdd(priceBooks, g.Arg("book").Field("Price"));
					}

					g = PriceTotaller.Internal.Method<decimal>("AveragePrice");
					{
						g.Return(priceBooks / countBooks);
					}
				}

				// Class to test the book database:
				TypeGen Test = ag.Class("Test");
				{
					// Print the title of the book.
					CodeGen g = Test.Static.Void("PrintTitle").Parameter(Book, "book");
					{
						g.WriteLine("   {0}", g.Arg("book").Field("Title"));
					}

					// Initialize the book database with some test books:
					g = Test.Static.Void("AddBooks").Parameter(BookDB, "bookDB");
					{
						Operand bookDB = g.Arg("bookDB");

						g.Invoke(bookDB, "AddBook", "The C Programming Language",
						  "Brian W. Kernighan and Dennis M. Ritchie", 19.95m, true);
						g.Invoke(bookDB, "AddBook", "The Unicode Standard 2.0",
						   "The Unicode Consortium", 39.95m, true);
						g.Invoke(bookDB, "AddBook", "The MS-DOS Encyclopedia",
						   "Ray Duncan", 129.95m, false);
						g.Invoke(bookDB, "AddBook", "Dogbert's Clues for the Clueless",
						   "Scott Adams", 12.00m, true);
					}

					// Execution starts here.
					g = Test.Static.Void("Main");
					{
						Operand bookDB = g.Local(Exp.New(BookDB));

						// Initialize the database with some books:
						g.Invoke(Test, "AddBooks", bookDB);

						// Print all the titles of paperbacks:
						g.WriteLine("Paperback Book Titles:");
						// Create a new delegate object associated with the static 
						// method Test.PrintTitle:
						g.Invoke(bookDB, "ProcessPaperbackBooks", Exp.NewDelegate(ProcessBookDelegate, Test, "PrintTitle"));

						// Get the average price of a paperback by using
						// a PriceTotaller object:
						Operand totaller = g.Local(Exp.New(PriceTotaller));
						// Create a new delegate object associated with the nonstatic 
						// method AddBookToTotal on the object totaller:
						g.Invoke(bookDB, "ProcessPaperbackBooks", Exp.NewDelegate(ProcessBookDelegate, totaller, "AddBookToTotal"));
						g.WriteLine("Average Paperback Book Price: ${0:#.##}",
						   totaller.Invoke("AveragePrice"));
					}
				}
			}
		}

		// example based on the MSDN Delegates Sample (compose.cs)
		public static void GenCompose(AssemblyGen ag)
		{
			TypeGen MyDelegate = ag.DelegateVoid("MyDelegate").Parameter<string>("string");

			TypeGen MyClass = ag.Class("MyClass");
			{
				CodeGen g = MyClass.Public.Static.Void("Hello").Parameter<string>("s");
				{
					g.WriteLine("  Hello, {0}!", g.Arg("s"));
				}

				g = MyClass.Public.Static.Void("Goodbye").Parameter<string>("s");
				{
					g.WriteLine("  Goodbye, {0}!", g.Arg("s"));
				}

				g = MyClass.Public.Static.Void("Main");
				{
					Operand a = g.Local(), b = g.Local(), c = g.Local(), d = g.Local();

					// Create the delegate object a that references 
					// the method Hello:
					g.Assign(a, Exp.NewDelegate(MyDelegate, MyClass, "Hello"));
					// Create the delegate object b that references 
					// the method Goodbye:
					g.Assign(b, Exp.NewDelegate(MyDelegate, MyClass, "Goodbye"));
					// The two delegates, a and b, are composed to form c, 
					// which calls both methods in order:
					g.Assign(c, a + b);
					// Remove a from the composed delegate, leaving d, 
					// which calls only the method Goodbye:
					g.Assign(d, c - a);

					g.WriteLine("Invoking delegate a:");
					g.InvokeDelegate(a, "A");
					g.WriteLine("Invoking delegate b:");
					g.InvokeDelegate(b, "B");
					g.WriteLine("Invoking delegate c:");
					g.InvokeDelegate(c, "C");
					g.WriteLine("Invoking delegate d:");
					g.InvokeDelegate(d, "D");
				}
			}
		}
	}
}
