using Domain.Interfaces.Repository.Admin;
using Domain.Interfaces.Service;
using Domain.Models.Admin;
using Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;

        public QuestionService(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public async Task<IEnumerable<QuestionTypeDto>> GetQuestionTypesAsync()
        {
            return await _questionRepository.GetQuestionTypesAsync();
        }

        public async Task<IEnumerable<QuestionDto>> GetSectionQuestionsAsync(int sectionId)
        {
            return await _questionRepository.GetSectionQuestionsAsync(sectionId);
        }

        public async Task<QuestionDetailDto> GetQuestionDetailsAsync(int questionId)
        {
            var question = await _questionRepository.GetQuestionDetailsAsync(questionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {questionId} not found");
            }
            return question;
        }

        public async Task<int> CreateQuestionAsync(QuestionCreateDto question, string createdBy)
        {
            ValidateQuestion(question);
            return await _questionRepository.CreateQuestionAsync(question, createdBy);
        }

        public async Task<bool> UpdateQuestionAsync(QuestionUpdateDto question, string modifiedBy)
        {
            ValidateQuestion(question);

            var existingQuestion = await _questionRepository.GetQuestionDetailsAsync(question.Id);
            if (existingQuestion == null)
            {
                throw new NotFoundException($"Question with ID {question.Id} not found");
            }

            return await _questionRepository.UpdateQuestionAsync(question, modifiedBy);
        }

        private void ValidateQuestion(QuestionCreateDto question)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                throw new ArgumentException("Question text is required");
            }

            if (question.QuestionTypeId <= 0)
            {
                throw new ArgumentException("Invalid question type");
            }

            // Add additional validation based on question type
            // For example, rating questions should have Min and Max values
        }

        private void ValidateQuestion(QuestionUpdateDto question)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                throw new ArgumentException("Question text is required");
            }

            if (question.QuestionTypeId <= 0)
            {
                throw new ArgumentException("Invalid question type");
            }

            // Add additional validation based on question type
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, string deletedBy)
        {
            var existingQuestion = await _questionRepository.GetQuestionDetailsAsync(questionId);
            if (existingQuestion == null)
            {
                throw new NotFoundException($"Question with ID {questionId} not found");
            }

            return await _questionRepository.DeleteQuestionAsync(questionId, deletedBy);
        }

        public async Task<bool> ReorderQuestionsAsync(int sectionId, IEnumerable<QuestionOrderDto> questionOrders, string modifiedBy)
        {
            if (questionOrders == null || !questionOrders.Any())
            {
                throw new ArgumentException("Question orders cannot be empty");
            }

            // Validate all question IDs exist and belong to the section
            var existingQuestions = await _questionRepository.GetSectionQuestionsAsync(sectionId);
            var existingQuestionIds = existingQuestions.Select(q => q.Id).ToHashSet();

            foreach (var order in questionOrders)
            {
                if (!existingQuestionIds.Contains(order.QuestionId))
                {
                    throw new ArgumentException($"Question with ID {order.QuestionId} does not exist or does not belong to the section");
                }
            }

            return await _questionRepository.ReorderQuestionsAsync(sectionId, questionOrders, modifiedBy);
        }

        public async Task<IEnumerable<QuestionResponseChoiceDto>> GetQuestionChoicesAsync(int questionId)
        {
            // Verify the question exists
            var question = await _questionRepository.GetQuestionDetailsAsync(questionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {questionId} not found");
            }

            return await _questionRepository.GetQuestionChoicesAsync(questionId);
        }

        public async Task<int> AddQuestionChoiceAsync(QuestionChoiceCreateDto choice, string createdBy)
        {
            // Verify the question exists
            var question = await _questionRepository.GetQuestionDetailsAsync(choice.QuestionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {choice.QuestionId} not found");
            }

            // Validate the choice
            if (string.IsNullOrWhiteSpace(choice.Text))
            {
                throw new ArgumentException("Choice text is required");
            }

            return await _questionRepository.AddQuestionChoiceAsync(choice, createdBy);
        }

        public async Task<bool> UpdateQuestionChoiceAsync(QuestionChoiceUpdateDto choice, string modifiedBy)
        {
            // Validate the choice
            if (string.IsNullOrWhiteSpace(choice.Text))
            {
                throw new ArgumentException("Choice text is required");
            }

            return await _questionRepository.UpdateQuestionChoiceAsync(choice, modifiedBy);
        }

        public async Task<bool> DeleteQuestionChoiceAsync(int choiceId, string deletedBy)
        {
            return await _questionRepository.DeleteQuestionChoiceAsync(choiceId, deletedBy);
        }

        public async Task<IEnumerable<MatrixRowDto>> GetMatrixRowsAsync(int questionId)
        {
            // Verify the question exists and is a matrix question
            var question = await _questionRepository.GetQuestionDetailsAsync(questionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {questionId} not found");
            }

            return await _questionRepository.GetMatrixRowsAsync(questionId);
        }

        public async Task<IEnumerable<MatrixColumnDto>> GetMatrixColumnsAsync(int questionId)
        {
            // Verify the question exists and is a matrix question
            var question = await _questionRepository.GetQuestionDetailsAsync(questionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {questionId} not found");
            }

            return await _questionRepository.GetMatrixColumnsAsync(questionId);
        }

        public async Task<int> AddMatrixRowAsync(MatrixRowDto row, string createdBy)
        {
            // Verify the question exists and is a matrix question
            var question = await _questionRepository.GetQuestionDetailsAsync(row.QuestionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {row.QuestionId} not found");
            }

            if (question.QuestionTypeName != "Matrix")
            {
                throw new ArgumentException("This question is not a matrix question");
            }

            // Validate the row
            if (string.IsNullOrWhiteSpace(row.Text))
            {
                throw new ArgumentException("Row text is required");
            }

            return await _questionRepository.AddMatrixRowAsync(row, createdBy);
        }

        public async Task<int> AddMatrixColumnAsync(MatrixColumnDto column, string createdBy)
        {
            // Verify the question exists and is a matrix question
            var question = await _questionRepository.GetQuestionDetailsAsync(column.QuestionId);
            if (question == null)
            {
                throw new NotFoundException($"Question with ID {column.QuestionId} not found");
            }

            if (question.QuestionTypeName != "Matrix")
            {
                throw new ArgumentException("This question is not a matrix question");
            }

            // Validate the column
            if (string.IsNullOrWhiteSpace(column.Text))
            {
                throw new ArgumentException("Column text is required");
            }

            return await _questionRepository.AddMatrixColumnAsync(column, createdBy);
        }

        public async Task<bool> DeleteMatrixRowAsync(int rowId, string deletedBy)
        {
            return await _questionRepository.DeleteMatrixRowAsync(rowId, deletedBy);
        }

        public async Task<bool> DeleteMatrixColumnAsync(int columnId, string deletedBy)
        {
            return await _questionRepository.DeleteMatrixColumnAsync(columnId, deletedBy);
        }
    }
}
