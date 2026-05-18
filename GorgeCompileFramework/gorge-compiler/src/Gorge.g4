grammar Gorge;
import GorgeLexerRules, GorgeExpression, GorgeStatement;

@header {
#pragma warning disable 3021
}

options{ 
    language = CSharp;
}

/*
    枚举的识别可能是和对象引用是混合的，需要对表来确定是枚举还是对象，可能需要回填
    是否要求先声明后使用？
*/

/*
    源文件，基本编译单元
*/
sourceFile
    : (classDeclaration | enumDeclaration | interfaceDeclaration | usingStatement | namespaceStatement)*
    ;

enumDeclaration
    : classModifier* Enum Identifier LeftCurlyBracket enumConstant (Comma enumConstant)* RightCurlyBracket
    ;

enumConstant
    : annotation* Identifier
    ;
 
// 类定义
classDeclaration
    : annotation* classModifier* Class Identifier genericsDeclaration? superclass? superInterfaces? LeftCurlyBracket classBody RightCurlyBracket
    ;

// using语句
usingStatement
    : Using (Identifier '=')? expression ';'
    ;

namespaceStatement
    : Namespace Identifier ('.' Identifier)* ';'
    ;

interfaceDeclaration
    : classModifier* Interface Identifier LeftCurlyBracket methodDeclaration* RightCurlyBracket
    ;

// 类泛型定义
genericsDeclaration
    : LeftAngleBracket Identifier (Comma Identifier)*RightAngleBracket
    ;

// 类修饰符
classModifier
    : Native
    ;

// 超类声明结构    
superclass
    : Colon expression
    ;
    
superInterfaces
    : '::' expression (Comma expression)*
    ;

// 类内容
classBody
    : memberDeclaration*
    ;

memberDeclaration
    : fieldDeclaration
    | methodDeclaration
    | constructorDeclaration
    ;

/*
    构造方法声明
*/
constructorDeclaration
    : annotation* constructorModifier* Identifier parameterList (superClassConstructor)? (codeBlockList | ';')
    ;

superClassConstructor
    : Colon Super LeftParenthesis (expression (Comma expression)*)? RightParenthesis
    ;

// 构造方法修饰符
constructorModifier
    : Injector
    ;

// 方法声明
methodDeclaration
    : annotation* methodModifier* expression Identifier parameterList (codeBlockList | ';')
    ;

// 方法修饰符
methodModifier
    : Static
    ;

// 方法声明中的参数表
parameterList
    : LeftParenthesis (parameter (Comma parameter)*)? RightParenthesis
    ;

// 方法声明中的参数定义
parameter
    : expression Identifier
    ;

// int i;
// int i = 1;
// int i = inject;
// 字段定义式，可带赋值
fieldDeclaration : annotation* expression Identifier ('=' expression)? ';';

/*
    注解
*/
annotation 
    : metadata? annotationIdentifier genericType? annotationParameters? 
    ;
    
annotationIdentifier
    : '@' Identifier
    ;
    
/*
    注解参数表
*/
annotationParameters                               
    : LeftParenthesis (annotationParameter (Comma annotationParameter)*)? RightParenthesis
    ;
    
genericType
    : LeftAngleBracket expression RightAngleBracket
    ;

annotationParameter
    : Identifier '=' expression
    ;

metadata
    : LeftSquareBracket(metadataEntry (Comma metadataEntry)*)? RightSquareBracket
    ;
    
metadataEntry
    : expression Identifier ('=' expression)?
    ;