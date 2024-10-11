# assignment_3

> [!IMPORTANT]
> Individual work is done in seperate branches, see all [branches](https://github.com/RUC-MSc-CS-CIT-2024/assignment_3/branches) to view individual work.

GitHub: https://github.com/RUC-MSc-CS-CIT-2024/assignment_3

Group **cit11**: 
- Ida Hay Jørgensen (stud-ijoergense@ruc.dk)
- Julius Krüger Madsen (stud-juliusm@ruc.dk)
- Marek Laslo (stud-laslo@ruc.dk)
- Sofus Hilfling Nielsen (stud-sofusn@ruc.dk)

## Test Results

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
.\Assignment3.TestSuite\bin\Debug\net8.0\Assignment3.TestSuite.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 8.0.10)
[xUnit.net 00:00:00.03]   Discovering: Assignment3.TestSuite
[xUnit.net 00:00:00.06]   Discovered:  Assignment3.TestSuite
[xUnit.net 00:00:00.06]   Starting:    Assignment3.TestSuite
  Passed Assignment3TestSuite.Assignment3Tests.Request_UpdateCategotyValidIdAndBody_ChangedCategoryName [23 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_CreateWithPathId_StatusBadRequest [3 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_ReadCategories_StatusOkAndListOfCategoriesInBody [4 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWithoutDate_MissingDateError [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_CreateCategoryWithValidBodyArgument_CreateNewCategory [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Echo_RequestWithBody_ReturnsBody [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_ConnectionWithoutRequest_ShouldConnect [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_ReadCategoryWithValidId_StatusOkAndCategoryInBody [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWithUnknownMethod_IllegalMethodError [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWithoutMethod_MissingMethodError [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_DeleteWithOutPathId_StatusBadRequest [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_UpdateCategoryWithValidIdAndBody_StatusUpdated [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateUpdateEchoWithoutBody_MissingBodyError(method: "create") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateUpdateEchoWithoutBody_MissingBodyError(method: "update") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateUpdateEchoWithoutBody_MissingBodyError(method: "echo") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_ReadCategoryWithInvalidId_StatusNotFound [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_DeleteCategoryWithInvalidId_StatusNotFound [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestUpdateWithoutJsonBody_IllegalBodyError [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Request_DeleteCategoryWithValidId_RemoveCategory [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateReadUpdateDeleteWithoutResource_MissingResourceError(method: "delete") [1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateReadUpdateDeleteWithoutResource_MissingResourceError(method: "read") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateReadUpdateDeleteWithoutResource_MissingResourceError(method: "create") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestForCreateReadUpdateDeleteWithoutResource_MissingResourceError(method: "update") [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWhereDateIsNotUnixTime_IllegalDateError [2 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_UpdateWithOutPathId_StatusBadRequest [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWithInvalidPathId_StatusBadRequest [< 1 ms]
  Passed Assignment3TestSuite.Assignment3Tests.Constraint_RequestWithInvalidPath_StatusBadRequest [< 1 ms]
[xUnit.net 00:00:00.14]   Finished:    Assignment3.TestSuite
  Passed Assignment3TestSuite.Assignment3Tests.Request_UpdateCategotyInvalidId_NotFound [< 1 ms]

Test Run Successful.
Total tests: 28
     Passed: 28
 Total time: 0,3901 Seconds

```