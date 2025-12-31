use EntityTestDb

-- Create Department table
CREATE TABLE Department (
    id INT PRIMARY KEY IDENTITY(1,1),
    name VARCHAR(100) NOT NULL
);

-- Insert sample departments
INSERT INTO Department (name) VALUES ('IT'), ('Sales');

-- Update Employee table structure to add name and departmentId columns
ALTER TABLE Employee
ADD name VARCHAR(100) NULL,
    departmentId INT NULL;

-- Insert all employee data (assuming empty table)
INSERT INTO Employee (name, salary, departmentId) VALUES 
    ('Joe', 85000, 1),
    ('Henry', 80000, 2),
    ('Sam', 60000, 2),
    ('Max', 90000, 1),
    ('Janet', 69000, 1),
    ('Randy', 85000, 1),
    ('Will', 70000, 1);

-- Insert 5 sample employees (Id will be auto-generated if IDENTITY)
-- INSERT INTO Employee (Salary)
-- VALUES 
--     (86000),
--     (72000),
--     (68000),
--     (62000),
--     (78000);

-- Verify the inserted data
SELECT * FROM Employee;
SELECT * FROM Department;

-- Select Top 2 saalry from Employee table
-- select top 1 Salary as SecondHighestSalary from (
-- Select Distinct top 2 Salary from Employee order by Salary desc ) result
-- order by Salary asc;

-- 
-- Return null if only 1 row present
SELECT 
    (SELECT DISTINCT Salary 
     FROM Employee 
     ORDER BY Salary DESC 
     OFFSET 2 ROW 
     FETCH NEXT 1 ROW ONLY) AS SecondHighestSalary;


--Delete from Employee where id =1
--with cte


-- Using CTE with ROW_NUMBER - Returns NULL if no 2nd highest exists
SELECT 
    (SELECT Salary 
     FROM (SELECT Salary, ROW_NUMBER() OVER (ORDER BY Salary DESC) AS rn 
           FROM Employee) AS Result 
     WHERE rn = 2) AS SecondHighestSalary;

-- OR using MAX with CTE (cleaner)
WITH Result AS (
    SELECT Salary, ROW_NUMBER() OVER (ORDER BY Salary DESC) AS rn 
    FROM Employee
)
SELECT MAX(Salary) AS SecondHighestSalary 
FROM Result 
WHERE rn = 2;

-- Using CTE return top 3 high earners in each department (includes ties)
WITH Result AS (
    SELECT D.name as Department, E.name as Employee, Salary, 
           DENSE_RANK() OVER (PARTITION BY D.id ORDER BY Salary DESC) AS rn 
    FROM Employee E 
    JOIN Department D ON E.departmentId = D.id
)
SELECT Department, Employee, Salary
FROM Result 
WHERE rn <= 3
ORDER BY Department, Salary DESC;



With result AS (

 select D.name as Department , E.Name as Employee , Salary ,
    DENSE_RANK() OVER (Partition by D.id order by Salary DESC ) as rn
    from Employee E JOin Department D On E.departmentID = D.id 
 )
 SELECT Department, Employee , Salary
 From result 
 where rn <= 3 
 order by Department , Salary DESC


--select salary , DENSE_RANK() over (order by salary desc) as SecondHighestSalary from Employee
--select salary , ROW_NUMBER() over (order by salary ) as SecondHighestSalary from Employee
--Select salary , RANK() over (order by salary ) as SecondHighestSalary from Employee;


-- ========== LEAD and LAG Examples ==========

-- Example 1: LAG - Compare each employee's salary with the previous employee's salary (ordered by salary)
SELECT 
    E.name as Employee,
    D.name as Department,
    E.Salary,
    LAG(E.Salary) OVER (ORDER BY E.Salary DESC) AS PreviousEmployeeSalary,
    E.Salary - LAG(E.Salary) OVER (ORDER BY E.Salary DESC) AS SalaryDifference
FROM Employee E
JOIN Department D ON E.departmentId = D.id
ORDER BY E.Salary DESC;

-- Example 2: LEAD - Compare each employee's salary with the next employee's salary
SELECT 
    E.name as Employee,
    D.name as Department,
    E.Salary as CurrentSalary,
    LEAD(E.Salary) OVER (ORDER BY E.Salary DESC) AS NextEmployeeSalary,
    E.Salary - LEAD(E.Salary) OVER (ORDER BY E.Salary DESC) AS GapToNextEmployee
FROM Employee E
JOIN Department D ON E.departmentId = D.id
ORDER BY E.Salary DESC;

-- Example 3: LAG and LEAD within each department (PARTITION BY)
SELECT 
    D.name as Department,
    E.name as Employee,
    E.Salary,
    LAG(E.name) OVER (PARTITION BY D.id ORDER BY E.Salary DESC) AS PreviousEmployee,
    LAG(E.Salary) OVER (PARTITION BY D.id ORDER BY E.Salary DESC) AS PreviousSalary,
    LEAD(E.name) OVER (PARTITION BY D.id ORDER BY E.Salary DESC) AS NextEmployee,
    LEAD(E.Salary) OVER (PARTITION BY D.id ORDER BY E.Salary DESC) AS NextSalary
FROM Employee E
JOIN Department D ON E.departmentId = D.id
ORDER BY Department, E.Salary DESC;

-- Example 4: LAG with default value - Show salary increase from previous employee (or 0 if first)
SELECT 
    D.name as Department,
    E.name as Employee,
    E.Salary,
    LAG(E.Salary, 1, 0) OVER (PARTITION BY D.id ORDER BY E.Salary) AS PreviousSalary,
    E.Salary - LAG(E.Salary, 1, 0) OVER (PARTITION BY D.id ORDER BY E.Salary) AS SalaryIncrease
FROM Employee E
JOIN Department D ON E.departmentId = D.id
ORDER BY Department, E.Salary;

-- Example 5: LEAD with offset 2 - Compare with salary two positions ahead
SELECT 
    E.name as Employee,
    E.Salary as CurrentSalary,
    LEAD(E.Salary, 1) OVER (ORDER BY E.Salary DESC) AS NextSalary,
    LEAD(E.Salary, 2) OVER (ORDER BY E.Salary DESC) AS TwoPositionsAhead,
    LEAD(E.name, 2, 'None') OVER (ORDER BY E.Salary DESC) AS EmployeeTwoAhead
FROM Employee E
ORDER BY E.Salary DESC;



SELECT * FROM EMPLOYEE E    
WHERE 2 = (SELECT COUNT(DISTINCT E1.SALARY)    
FROM EMPLOYEE E1    
WHERE E1.SALARY>E.SALARY)   


-- ========== Create Employee1 and Department1 Tables ==========

-- Create Department1 table
CREATE TABLE Department1 (
    deptid INT PRIMARY KEY,
    deptname VARCHAR(100) NOT NULL
);

-- Insert Department1 data
INSERT INTO Department1 (deptid, deptname) VALUES 
    (1, 'IT'),
    (2, 'Admin');

-- Create Employee1 table
CREATE TABLE Employee1 (
    empid INT PRIMARY KEY,
    empname VARCHAR(100) NOT NULL,
    managerid INT NULL,
    deptid INT NULL,
    salary DECIMAL(10,2) NULL,
    DOB DATETIME NULL
);

-- Insert Employee1 data
INSERT INTO Employee1 (empid, empname, managerid, deptid, salary, DOB) VALUES
    (1, 'emp 1', 0, 1, 6000, '1982-08-06 00:00:00.000'),
    (2, 'emp 2', 0, 5, 6000, '1982-07-11 00:00:00.000'),
    (3, 'emp 3', 1, 1, 2000, '1983-11-21 00:00:00.000'),
    (13, 'emp 13', 2, 5, 2000, '1984-03-09 00:00:00.000'),
    (11, 'emp 11', 2, 1, 2000, '1989-07-23 00:00:00.000'),
    (9, 'emp 9', 1, 5, 3000, '1990-09-11 00:00:00.000'),
    (8, 'emp 8', 3, 1, 3500, '1990-05-15 00:00:00.000'),
    (7, 'emp 7', 2, 5, NULL, NULL);

-- Verify the data
SELECT * FROM Employee1 ORDER BY empid;
SELECT * FROM Department1;

Select * from Employee E
WHERE 2 = (Select count(DISTINCT E1.Salary) from Employee E1 where E1.Salary > E.Salary)
--Select count(DISTINCT E1.Salary) from Employee E1 JOIN Employee E2 on E1.Salary >= E2.Salary



Select * from Employee

Select DepartmentID from Employee
INTERSECT
Select id from Department;


Select * from Employee
Order by id desc 

Select * from Employee
Order by id desc 
OFFSET 1 ROWS
FETCH NEXT 2 ROWS ONLY;


-- CROSS APply 
select E.name , E.salary , D.name as DepartmentName from Employee E
CROSS APPLY Department D
WHERE E.departmentId = D.id;

-- Turn on execution plan display
SET SHOWPLAN_TEXT ON;
GO

-- Your query here
SELECT E.name, E.salary, D.name as DepartmentName 
FROM Employee E
CROSS APPLY Department D
WHERE E.departmentId = D.id;
GO

-- Turn off
SET SHOWPLAN_TEXT OFF;
GO


-- ========== Find Products That Have Never Been Ordered ==========

-- Create Customers table
CREATE TABLE Customers (
    CustomerId INT PRIMARY KEY IDENTITY(1,1),
    CustomerName VARCHAR(100) NOT NULL,
    Email VARCHAR(100)
);

-- Use existing Product table (id, Name, Description, Price, RowVersion)
-- If Product table doesn't exist, create it first
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        id INT PRIMARY KEY IDENTITY(1,1),
        Name VARCHAR(100) NOT NULL,
        Description VARCHAR(500),
        Price DECIMAL(10,2),
        RowVersion ROWVERSION
    );
    
    -- Insert sample products
    INSERT INTO Products (Name, Description, Price) VALUES
        ('Laptop', 'High performance laptop', 999.99),
        ('Mouse', 'Wireless mouse', 29.99),
        ('Keyboard', 'Mechanical keyboard', 79.99),
        ('Monitor', '27-inch display', 299.99),
        ('Webcam', 'HD webcam', 89.99);
END

-- Create Orders table (references existing Products table)
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    ProductId INT NOT NULL,  -- References Products.id
    OrderDate DATETIME DEFAULT GETDATE(),
    Quantity INT,
    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    FOREIGN KEY (ProductId) REFERENCES Products(id)
);

-- Insert sample customers
INSERT INTO Customers (CustomerName, Email) VALUES
    ('John Doe', 'john@example.com'),
    ('Jane Smith', 'jane@example.com'),
    ('Bob Wilson', 'bob@example.com');

-- Insert sample orders using existing Product IDs
-- Assuming Product table has some entries already
-- Insert Orders (adjust ProductId based on your existing Product.id values)
INSERT INTO Orders (CustomerId, ProductId, OrderDate, Quantity) VALUES
    (1, 1, '2024-12-01', 1),  -- John ordered Product with id=1
    (1, 2, '2024-12-01', 2),  -- John ordered Product with id=2
    (2, 1, '2024-12-10', 1),  -- Jane ordered Product with id=1
    (2, 3, '2024-12-10', 1),  -- Jane ordered Product with id=3
    (3, 2, '2024-12-15', 3);  -- Bob ordered Product with id=2

-- Verify data
SELECT * FROM Customers;
SELECT * FROM Products;  -- Products table
SELECT * FROM Orders;

-- ========== Query 1: Using LEFT JOIN (Most common approach) ==========
SELECT P.id, P.Name, P.Price
FROM Products P
LEFT JOIN Orders O ON P.id = O.ProductId
WHERE O.OrderId IS NULL;

-- ========== Query 2: Using NOT EXISTS (Better performance for large datasets) ==========
SELECT P.id, P.Name, P.Price
FROM Products P
WHERE NOT EXISTS (
    SELECT 1 
    FROM Orders O 
    WHERE O.ProductId = P.id
);

-- ========== Query 3: Using NOT IN (Simple but slower) ==========
SELECT P.id, P.Name, P.Price
FROM Products P
WHERE P.id NOT IN (
    SELECT DISTINCT ProductId 
    FROM Orders
);

-- ========== Query 4: Using EXCEPT (Set-based approach) ==========
SELECT id, Name, Price
FROM Products
WHERE id IN (
    SELECT id FROM Products
    EXCEPT
    SELECT ProductId FROM Orders
);

-- Expected Result: Products that have never been ordered

Select * from Products
Select * from ProductDetails


Select * from Orders
where ProductId not in (

Select Distinct Id from Products   ) 

-- check indexes 
SELECT *
FROM sys.dm_db_index_usage_stats
WHERE database_id = DB_ID();
GO