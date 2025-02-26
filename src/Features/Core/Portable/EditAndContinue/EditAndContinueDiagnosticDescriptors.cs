﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal static class EditAndContinueDiagnosticDescriptors
    {
        private const int GeneralDiagnosticBaseId = 1000;
        private const int ModuleDiagnosticBaseId = 2000;
        private static readonly int s_diagnosticBaseIndex;

        private static readonly LocalizableResourceString s_rudeEditLocString;
        private static readonly LocalizableResourceString s_encLocString;
        private static readonly LocalizableResourceString s_encDisallowedByProjectLocString;

        private static readonly ImmutableArray<DiagnosticDescriptor> s_descriptors;

        // descriptors for diagnostics reported by the debugger:
        private static Dictionary<ManagedEditAndContinueAvailabilityStatus, DiagnosticDescriptor> s_lazyModuleDiagnosticDescriptors;
        private static readonly object s_moduleDiagnosticDescriptorsGuard;

        static EditAndContinueDiagnosticDescriptors()
        {
            s_moduleDiagnosticDescriptorsGuard = new object();

            s_rudeEditLocString = new LocalizableResourceString(nameof(FeaturesResources.RudeEdit), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            s_encLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            s_encDisallowedByProjectLocString = new LocalizableResourceString(nameof(FeaturesResources.EditAndContinueDisallowedByProject), FeaturesResources.ResourceManager, typeof(FeaturesResources));

            var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

            void add(int index, int id, string resourceName, LocalizableResourceString title, DiagnosticSeverity severity)
            {
                if (index >= builder.Count)
                {
                    builder.Count = index + 1;
                }

                builder[index] = new DiagnosticDescriptor(
                    $"ENC{id:D4}",
                    title,
                    messageFormat: new LocalizableResourceString(resourceName, FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                    DiagnosticCategory.EditAndContinue,
                    severity,
                    isEnabledByDefault: true,
                    customTags: DiagnosticCustomTags.EditAndContinue);
            }

            void AddRudeEdit(RudeEditKind kind, string resourceName)
                => add(GetDescriptorIndex(kind), (int)kind, resourceName, s_rudeEditLocString, DiagnosticSeverity.Error);

            void AddGeneralDiagnostic(EditAndContinueErrorCode code, string resourceName, DiagnosticSeverity severity = DiagnosticSeverity.Error)
                => add(GetDescriptorIndex(code), GeneralDiagnosticBaseId + (int)code, resourceName, s_encLocString, severity);

            //
            // rude edits
            //

            AddRudeEdit(RudeEditKind.InsertAroundActiveStatement, nameof(FeaturesResources.Adding_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeleteAroundActiveStatement, nameof(FeaturesResources.Deleting_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeleteActiveStatement, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdateAroundActiveStatement, nameof(FeaturesResources.Updating_a_0_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdateExceptionHandlerOfActiveTry, nameof(FeaturesResources.Modifying_a_catch_finally_handler_with_an_active_statement_in_the_try_block_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdateTryOrCatchWithActiveFinally, nameof(FeaturesResources.Modifying_a_try_catch_finally_statement_when_the_finally_block_is_active_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdateCatchHandlerAroundActiveStatement, nameof(FeaturesResources.Modifying_a_catch_handler_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.Update, nameof(FeaturesResources.Updating_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ModifiersUpdate, nameof(FeaturesResources.Updating_the_modifiers_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.VarianceUpdate, nameof(FeaturesResources.Updating_the_variance_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.TypeUpdate, nameof(FeaturesResources.Updating_the_type_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InitializerUpdate, nameof(FeaturesResources.Updating_the_initializer_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.FixedSizeFieldUpdate, nameof(FeaturesResources.Updating_the_size_of_a_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.EnumUnderlyingTypeUpdate, nameof(FeaturesResources.Updating_the_underlying_type_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.BaseTypeOrInterfaceUpdate, nameof(FeaturesResources.Updating_the_base_class_and_or_base_interface_s_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.TypeKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_a_type_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.AccessorKindUpdate, nameof(FeaturesResources.Updating_the_kind_of_an_property_event_accessor_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeclareAliasUpdate, nameof(FeaturesResources.Updating_the_alias_of_Declare_Statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeclareLibraryUpdate, nameof(FeaturesResources.Updating_the_library_name_of_Declare_Statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.FieldKindUpdate, nameof(FeaturesResources.Updating_a_field_to_an_event_or_vice_versa_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.Renamed, nameof(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.Insert, nameof(FeaturesResources.Adding_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertVirtual, nameof(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertOverridable, nameof(FeaturesResources.Adding_a_MustOverride_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertExtern, nameof(FeaturesResources.Adding_an_extern_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertDllImport, nameof(FeaturesResources.Adding_an_imported_method_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertOperator, nameof(FeaturesResources.Adding_a_user_defined_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertIntoStruct, nameof(FeaturesResources.Adding_0_into_a_1_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertIntoClassWithLayout, nameof(FeaturesResources.Adding_0_into_a_class_with_explicit_or_sequential_layout_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertGenericMethod, nameof(FeaturesResources.Adding_a_generic_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.Move, nameof(FeaturesResources.Moving_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.Delete, nameof(FeaturesResources.Deleting_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.MethodBodyAdd, nameof(FeaturesResources.Adding_a_method_body_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.MethodBodyDelete, nameof(FeaturesResources.Deleting_a_method_body_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.GenericMethodUpdate, nameof(FeaturesResources.Modifying_a_generic_method_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.GenericMethodTriviaUpdate, nameof(FeaturesResources.Modifying_whitespace_or_comments_in_a_generic_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.GenericTypeUpdate, nameof(FeaturesResources.Modifying_a_method_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.GenericTypeTriviaUpdate, nameof(FeaturesResources.Modifying_whitespace_or_comments_in_0_inside_the_context_of_a_generic_type_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.GenericTypeInitializerUpdate, nameof(FeaturesResources.Modifying_the_initializer_of_0_in_a_generic_type_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, nameof(FeaturesResources.Adding_a_constructor_to_a_type_with_a_field_or_property_initializer_that_contains_an_anonymous_function_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.RenamingCapturedVariable, nameof(FeaturesResources.Renaming_a_captured_variable_from_0_to_1_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.StackAllocUpdate, nameof(FeaturesResources.Modifying_0_which_contains_the_stackalloc_operator_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ExperimentalFeaturesEnabled, nameof(FeaturesResources.Modifying_source_with_experimental_language_features_enabled_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.AwaitStatementUpdate, nameof(FeaturesResources.Updating_a_complex_statement_containing_an_await_expression_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingAccessibility, nameof(FeaturesResources.Changing_visibility_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.CapturingVariable, nameof(FeaturesResources.Capturing_variable_0_that_hasn_t_been_captured_before_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.NotCapturingVariable, nameof(FeaturesResources.Ceasing_to_capture_variable_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeletingCapturedVariable, nameof(FeaturesResources.Deleting_captured_variable_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingCapturedVariableType, nameof(FeaturesResources.Changing_the_type_of_a_captured_variable_0_previously_of_type_1_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingCapturedVariableScope, nameof(FeaturesResources.Changing_the_declaration_scope_of_a_captured_variable_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingLambdaParameters, nameof(FeaturesResources.Changing_the_parameters_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingLambdaReturnType, nameof(FeaturesResources.Changing_the_return_type_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingQueryLambdaType, nameof(FeaturesResources.Changing_the_type_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.AccessingCapturedVariableInLambda, nameof(FeaturesResources.Accessing_captured_variable_0_that_hasn_t_been_accessed_before_in_1_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.NotAccessingCapturedVariableInLambda, nameof(FeaturesResources.Ceasing_to_access_captured_variable_0_in_1_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertLambdaWithMultiScopeCapture, nameof(FeaturesResources.Adding_0_that_accesses_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.DeleteLambdaWithMultiScopeCapture, nameof(FeaturesResources.Removing_0_that_accessed_captured_variables_1_and_2_declared_in_different_scopes_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ActiveStatementLambdaRemoved, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_will_prevent_the_debug_session_from_continuing));
            // TODO: change the error message to better explain what's going on
            AddRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementUpdate, nameof(FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.PartiallyExecutedActiveStatementDelete, nameof(FeaturesResources.Removing_0_that_contains_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertFile, nameof(FeaturesResources.Adding_a_new_file_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, nameof(FeaturesResources.Updating_async_or_iterator_modifier_around_an_active_statement_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdatingStateMachineMethodMissingAttribute, nameof(FeaturesResources.Attribute_0_is_missing_Updating_an_async_method_or_an_iterator_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.SwitchBetweenLambdaAndLocalFunction, nameof(FeaturesResources.Switching_between_lambda_and_local_function_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, nameof(FeaturesResources.Adding_method_with_explicit_interface_specifier_will_prevernt_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertIntoInterface, nameof(FeaturesResources.Adding_0_into_an_interface_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertLocalFunctionIntoInterfaceMethod, nameof(FeaturesResources.Adding_0_into_an_interface_method_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InternalError, nameof(FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_due_to_internal_error));
            AddRudeEdit(RudeEditKind.ChangingFromAsynchronousToSynchronous, nameof(FeaturesResources.Changing_0_from_asynchronous_to_synchronous_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingStateMachineShape, nameof(FeaturesResources.Changing_0_to_1_will_prevent_the_debug_session_from_continuing_because_it_changes_the_shape_of_the_state_machine));
            AddRudeEdit(RudeEditKind.ComplexQueryExpression, nameof(FeaturesResources.Modifying_0_which_contains_an_Aggregate_Group_By_or_Join_query_clauses_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.MemberBodyInternalError, nameof(FeaturesResources.Modifying_body_of_member_will_prevent_the_debug_session_from_continuing_due_to_internal_error));
            AddRudeEdit(RudeEditKind.MemberBodyTooBig, nameof(FeaturesResources.Modifying_body_of_member_will_prevent_the_debug_session_from_continuing_because_the_body_has_too_many_statements));
            AddRudeEdit(RudeEditKind.SourceFileTooBig, nameof(FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_because_the_file_is_too_big));
            AddRudeEdit(RudeEditKind.InsertIntoGenericType, nameof(FeaturesResources.Adding_0_into_a_generic_type_will_prevent_the_debug_session_from_continuing));

            AddRudeEdit(RudeEditKind.ImplementRecordParameterAsReadOnly, nameof(FeaturesResources.Implementing_a_record_positional_parameter_0_as_read_only_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ImplementRecordParameterWithSet, nameof(FeaturesResources.Implementing_a_record_positional_parameter_0_with_a_set_accessor_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch, nameof(FeaturesResources.Explicitly_implemented_methods_of_records_must_have_parameter_names_that_match_the_compiler_generated_equivalent_0));

            AddRudeEdit(RudeEditKind.NotSupportedByRuntime, nameof(FeaturesResources.Edit_and_continue_is_not_supported_by_the_runtime));
            AddRudeEdit(RudeEditKind.MakeMethodAsync, nameof(FeaturesResources.Making_a_method_async_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.MakeMethodIterator, nameof(FeaturesResources.Making_a_method_an_iterator_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertNotSupportedByRuntime, nameof(FeaturesResources.Adding_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingAttributesNotSupportedByRuntime, nameof(FeaturesResources.Updating_the_attributes_of_0_is_not_supported_by_the_runtime));
            AddRudeEdit(RudeEditKind.ChangingParameterTypes, nameof(FeaturesResources.Changing_parameter_types_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingTypeParameters, nameof(FeaturesResources.Changing_type_parameters_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ChangingConstraints, nameof(FeaturesResources.Changing_constraints_of_0_will_prevent_the_debug_session_from_continuing));

            AddRudeEdit(RudeEditKind.ChangeImplicitMainReturnType, FeaturesResources.An_update_that_causes_the_return_type_of_implicit_main_to_change_will_prevent_the_debug_session_from_continuing);

            // VB specific
            AddRudeEdit(RudeEditKind.HandlesClauseUpdate, nameof(FeaturesResources.Updating_the_Handles_clause_of_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.ImplementsClauseUpdate, nameof(FeaturesResources.Updating_the_Implements_clause_of_a_0_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.InsertHandlesClause, nameof(FeaturesResources.Adding_0_with_the_Handles_clause_will_prevent_the_debug_session_from_continuing));
            AddRudeEdit(RudeEditKind.UpdateStaticLocal, nameof(FeaturesResources.Modifying_0_which_contains_a_static_variable_will_prevent_the_debug_session_from_continuing));

            //
            // other Roslyn reported diagnostics:
            //

            s_diagnosticBaseIndex = builder.Count;

            AddGeneralDiagnostic(EditAndContinueErrorCode.ErrorReadingFile, nameof(FeaturesResources.ErrorReadingFile));
            AddGeneralDiagnostic(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError, nameof(FeaturesResources.CannotApplyChangesUnexpectedError));
            AddGeneralDiagnostic(EditAndContinueErrorCode.ChangesDisallowedWhileStoppedAtException, nameof(FeaturesResources.ChangesDisallowedWhileStoppedAtException));
            AddGeneralDiagnostic(EditAndContinueErrorCode.DocumentIsOutOfSyncWithDebuggee, nameof(FeaturesResources.DocumentIsOutOfSyncWithDebuggee), DiagnosticSeverity.Warning);
            AddGeneralDiagnostic(EditAndContinueErrorCode.UnableToReadSourceFileOrPdb, nameof(FeaturesResources.UnableToReadSourceFileOrPdb), DiagnosticSeverity.Warning);

            s_descriptors = builder.ToImmutable();
        }

        internal static ImmutableArray<DiagnosticDescriptor> GetDescriptors()
            => s_descriptors.WhereAsArray(d => d != null);

        internal static DiagnosticDescriptor GetDescriptor(RudeEditKind kind)
            => s_descriptors[GetDescriptorIndex(kind)];

        internal static DiagnosticDescriptor GetDescriptor(EditAndContinueErrorCode errorCode)
            => s_descriptors[GetDescriptorIndex(errorCode)];

        internal static DiagnosticDescriptor GetModuleDiagnosticDescriptor(ManagedEditAndContinueAvailabilityStatus status)
        {
            lock (s_moduleDiagnosticDescriptorsGuard)
            {
                s_lazyModuleDiagnosticDescriptors ??= new Dictionary<ManagedEditAndContinueAvailabilityStatus, DiagnosticDescriptor>();

                if (!s_lazyModuleDiagnosticDescriptors.TryGetValue(status, out var descriptor))
                {
                    s_lazyModuleDiagnosticDescriptors.Add(status, descriptor = new DiagnosticDescriptor(
                        $"ENC{ModuleDiagnosticBaseId + (int)status:D4}",
                        s_encLocString,
                        s_encDisallowedByProjectLocString,
                        DiagnosticCategory.EditAndContinue,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        customTags: DiagnosticCustomTags.EditAndContinue));
                }

                return descriptor;
            }
        }

        private static int GetDescriptorIndex(RudeEditKind kind)
            => (int)kind;

        private static int GetDescriptorIndex(EditAndContinueErrorCode errorCode)
            => s_diagnosticBaseIndex + (int)errorCode;
    }
}
