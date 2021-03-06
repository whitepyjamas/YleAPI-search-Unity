﻿using System;

using GUI.Details;
using GUI.List;
using Yle;

namespace GUI.Search
{
    public class SearchPresenter
    {
        #region - LifeCycle
        public SearchPresenter(IManager m_Manager, IRequestBuilder m_RequestBuilder, ISearchView m_SearchView,
                               ISearchModel m_SearchModel)
        {
            this.m_Manager = m_Manager;
            this.m_RequestBuilder = m_RequestBuilder;
            this.m_SearchView = m_SearchView;
            this.m_SearchModel = m_SearchModel;

            m_SearchViewModel = new ViewModel<ProgramData>();

            m_SearchView.OnSearch += StartSearch;
            m_SearchView.OnDetails += OnDetailsButton;
            m_SearchView.onReachingEnd += ContinueList;
            m_SearchView.searchViewModel = m_SearchViewModel;
        }
        #endregion

        #region - Dependencies
        readonly IManager m_Manager;
        readonly ISearchView m_SearchView;
        readonly ISearchModel m_SearchModel;
        readonly IRequestBuilder m_RequestBuilder;
        readonly ViewModel<ProgramData> m_SearchViewModel;
        #endregion

        #region - State
        IDetailsView m_DetailsView;

        int m_CurrentSegment;
        bool m_SegmentPending;
        bool m_LastSegment = true;
        #endregion

        #region - Private
        int currentSegment {
            get { return m_CurrentSegment; }
            set {
                m_CurrentSegment = value;
                segmentPending = true;
            }
        }

        bool segmentPending {
            get { return m_SegmentPending; }
            set {
                m_SegmentPending = value;
                m_SearchView.ToggleActivityIndicator(value);
            }
        }

        void StartSearch()
        {
            if (segmentPending) {
                return;
            }

            m_SearchViewModel.Clear();
            m_CurrentSegment = -1;
            m_LastSegment = false;

            ContinueList();
        }

        void ContinueList()
        {
            if (m_LastSegment || segmentPending) {
                return;
            }

            var searchText = m_SearchView.searchText;
            if (string.IsNullOrEmpty(searchText)) {
                return;
            }

            currentSegment++;

            m_SearchModel.FindProgramsByTitle(
                searchText,
                offset: currentSegment * m_RequestBuilder.limitResults,
                onDone: OnAnswer
            );
        }

        void OnAnswer(Answer answer)
        {
            var receivedCount = (currentSegment + 1) * m_RequestBuilder.limitResults;
            m_LastSegment = answer.meta.count <= receivedCount;

            segmentPending = false;

            m_SearchViewModel.Append(answer.data);
        }

        void OnDetailsButton(string id)
        {
            var found = m_SearchViewModel.Find(data => string.CompareOrdinal(data.id, id) == 0);

            if (m_DetailsView == null) {
                CreateDetailsView();
            }

            // We are not using standard gui manager behaviour
            // because the details window is very likely to be reopened
            // probably later can do some window not closable by manager
            m_DetailsView.Set(found);
            m_DetailsView.Show();
            m_SearchView.Hide();
        }

        void CreateDetailsView()
        {
            m_DetailsView = m_Manager.OpenWindow<DetailsWindow>();

            m_DetailsView.onBackButton += () => {
                m_DetailsView.Hide();
                m_SearchView.Show();
            };
        }
        #endregion
    }
}
